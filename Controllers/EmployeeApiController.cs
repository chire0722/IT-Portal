// Project comment: EmployeeApiController.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using ClosedXML.Excel;
using System.Globalization;
using IT_Form.Helpers;
using IT_Form.Services;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireHROrIT")]
    [Route("api/employees")]
    [ApiController]
    [EnableRateLimiting("strict")]
    /// <summary>
    /// Handles web requests for the EmployeeApi area.
    /// </summary>
    public class EmployeeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly IFileMalwareScanner? _malwareScanner;

        public EmployeeApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISignatureIntegrityService integrityService,
            IFileMalwareScanner? malwareScanner = null)
        {
            _context = context;
            _userManager = userManager;
            _integrityService = integrityService;
            _malwareScanner = malwareScanner;
        }

        [Authorize(Policy = "RequireHR")]
        [HttpGet("import-template")]
        /// <summary>
        /// Runs the DownloadTemplate operation.
        /// </summary>
        public async Task<IActionResult> DownloadTemplate()
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var dbDepartments = await _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .Select(d => d.Name)
                .OrderBy(n => n)
                .ToListAsync();
            var dbCompanies = await CompanyScopeHelper.LoadAccessibleCompanyDisplayNamesAsync(
                _context,
                User,
                cancellationToken);

            using (var workbook = new XLWorkbook())
            {
                var refSheet = ExcelTemplateDropdownHelper.CreateReferenceSheet(workbook, "_Ref", hideSheet: true);
                var hasDepartmentOptions = ExcelTemplateDropdownHelper.AddNamedList(
                    workbook,
                    refSheet,
                    ExcelTemplateDropdownHelper.DepartmentOptionsDefinedName,
                    1,
                    "Active Departments",
                    dbDepartments);
                var hasCompanyOptions = ExcelTemplateDropdownHelper.AddNamedList(
                    workbook,
                    refSheet,
                    ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                    2,
                    "Active Companies",
                    dbCompanies);

                var worksheet = workbook.Worksheets.Add("Staff Template");

                // Headers
                string[] headers = {
                    "Employee ID",
                    "Full Name",
                    "Position",
                    "Email",
                    "Date Hire (DD/MM/YYYY)",
                    "Desk Number",
                    "Department",
                    "Contact Number",
                    "Reporting Manager",
                    "Company"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                if (hasDepartmentOptions)
                {
                    ExcelTemplateDropdownHelper.ApplyDropdownFromNamedList(
                        worksheet,
                        "G2:G1000",
                        ExcelTemplateDropdownHelper.DepartmentOptionsDefinedName,
                        "Select Department",
                        "Please select a department from the dropdown list.",
                        "Invalid Department",
                        "Please select a department from the dropdown list.");
                }

                if (hasCompanyOptions)
                {
                    ExcelTemplateDropdownHelper.ApplyDropdownFromNamedList(
                        worksheet,
                        "J2:J1000",
                        ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                        "Select Company",
                        "Please select a company from the dropdown list.",
                        "Invalid Company",
                        "Please select a company from the dropdown list.");
                }

                // Freeze first row
                worksheet.SheetView.FreezeRows(1);

                // Auto-fit columns
                ExcelTemplateDropdownHelper.SetColumnWidths(worksheet, 18, 30, 24, 32, 20, 18, 22, 18, 30, 32);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Legacy_Staff_Import_Template.xlsx");
                }
            }
        }

        [Authorize(Policy = "RequireHR")]
        [HttpPost("import")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        /// <summary>
        /// Runs the Import operation.
        /// </summary>
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!FileUploadHelper.IsValidExcelFile(file, out string errorMessage))
                return BadRequest(errorMessage);

            await MalwareScanHelper.EnsureCleanAsync(
                file,
                _malwareScanner,
                "employee import",
                HttpContext.RequestAborted);

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserName = currentUser?.UserName ?? currentUser?.Email ?? HttpContext?.User?.Identity?.Name ?? "System Migration";
            var signature = currentUser?.Signature;
            if (string.IsNullOrWhiteSpace(signature))
            {
                return BadRequest("No HR signature found. Please set up your profile signature before importing employees.");
            }

            var signerName = !string.IsNullOrWhiteSpace(currentUser?.FullName)
                ? currentUser.FullName
                : currentUserName;

            int totalProcessed = 0;
            int successfullyInserted = 0;
            int skippedDuplicates = 0;
            int invalidDateRows = 0;
            int invalidCompanyRows = 0;

            var newEmployees = new List<OnboardingRequest>();
            var existingEmployeeIds = await _context.OnboardingRequests
                .Where(e => e.EmployeeID != null)
                .Select(e => e.EmployeeID)
                .ToListAsync();
            var activeCompanies = await _context.Companies
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream, HttpContext?.RequestAborted ?? CancellationToken.None);
                stream.Position = 0;
                using (var workbook = new XLWorkbook(stream))
                {
                    // Attempt to locate the specific staff template sheet, otherwise grab the first sheet that isn't the hidden _Ref sheet
                    var worksheet = workbook.TryGetWorksheet("Staff Template", out var ws) 
                        ? ws 
                        : workbook.Worksheets.FirstOrDefault(w => w.Name != "_Ref");

                    if (worksheet == null)
                        worksheet = workbook.Worksheet(1); // fallback

                    var range = worksheet.RangeUsed();
                    if (range == null)
                        return BadRequest("Uploaded file contains no data.");

                    var headerRow = range.FirstRowUsed();
                    var columnMap = BuildColumnMap(headerRow);
                    var rows = range.RowsUsed().Skip(1); // Skip header

                    foreach (var row in rows)
                    {
                        totalProcessed++;

                        var empNum = GetCellValue(row, columnMap, EmployeeIdHeaders, 1);
                        var fullName = GetCellValue(row, columnMap, FullNameHeaders, 2);
                        var position = GetCellValue(row, columnMap, PositionHeaders, 3);
                        var email = GetCellValue(row, columnMap, EmailHeaders, 4);
                        var dept = GetCellValue(row, columnMap, DepartmentHeaders, 6);
                        var deskNumber = GetCellValue(row, columnMap, DeskNumberHeaders, 0);
                        var contact = GetCellValue(row, columnMap, ContactNumberHeaders, 7);
                        var managerEmail = GetCellValue(row, columnMap, ReportingManagerHeaders, 8);
                        var company = GetCellValue(row, columnMap, CompanyHeaders, 9);

                        // Skip empty rows
                        if (string.IsNullOrEmpty(empNum) && string.IsNullOrEmpty(fullName))
                            continue;

                        // Deduplication: check by Employee Number
                        if (!string.IsNullOrEmpty(empNum) && existingEmployeeIds.Contains(empNum))
                        {
                            skippedDuplicates++;
                            continue;
                        }

                        // Parse Date
                        if (!ExcelImportDateHelper.TryReadOptionalDate(
                            row.Cell(GetColumnNumber(columnMap, DateHireHeaders, 5)),
                            out var joinDate,
                            out var joinDateError))
                        {
                            invalidDateRows++;
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(company))
                        {
                            var matchedCompany = CompanyImportMatchHelper.FindCompanyByReference(activeCompanies, company);
                            if (matchedCompany == null)
                            {
                                invalidCompanyRows++;
                                continue;
                            }

                            company = matchedCompany.DisplayName;
                        }

                        if (string.IsNullOrWhiteSpace(company) || !CompanyScopeHelper.CanAccessCompany(User, company))
                        {
                            invalidCompanyRows++;
                            continue;
                        }

                        var importedAt = DateTime.UtcNow;
                        var request = new OnboardingRequest
                        {
                            EmployeeID = empNum,
                            FullName = fullName,
                            Designation = position,
                            EmailAddress = email,
                            JoinDate = joinDate,
                            DeskNumber = deskNumber,
                            Department = dept,
                            ContactNumber = contact,
                            HODEmail = managerEmail,
                            Branch = company,
                            Status = RequestStatus.Completed,
                            RequestDate = importedAt,
                            CreatedAt = importedAt,
                            CompletedAt = importedAt,
                            CreatedByUserId = currentUserName,
                            RequesterSignature = signature,
                            RequesterSignerName = signerName,
                            RequesterSignedAt = importedAt
                        };

                        newEmployees.Add(request);
                        successfullyInserted++;

                        // Prevent duplicates within the same batch
                        if (!string.IsNullOrEmpty(empNum))
                            existingEmployeeIds.Add(empNum);
                    }
                }
            }

            if (newEmployees.Any())
            {
                _context.OnboardingRequests.AddRange(newEmployees);
                await _context.SaveChangesAsync();

                foreach (var employee in newEmployees)
                {
                    employee.RequesterSignatureHmac = _integrityService.ComputeHmac(
                        "OnboardingRequest",
                        employee.Id.ToString(CultureInfo.InvariantCulture),
                        employee.RequesterSignerName ?? signerName,
                        employee.RequesterSignedAt!.Value,
                        employee.RequesterSignature!);
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                TotalProcessed = totalProcessed,
                SuccessfullyInserted = successfullyInserted,
                SkippedDuplicates = skippedDuplicates,
                InvalidDateRows = invalidDateRows,
                InvalidCompanyRows = invalidCompanyRows
            });
        }
        [HttpGet("{id}/details")]
        /// <summary>
        /// Gets the data required by the GetEmployeeDetails operation.
        /// </summary>
        public async Task<IActionResult> GetEmployeeDetails(int id)
        {
            var employee = await _context.OnboardingRequests
                .Include(o => o.IssuedAssets)
                    .ThenInclude(ia => ia.ITAsset)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (employee == null)
                return NotFound("Employee not found.");

            if (!CompanyScopeHelper.CanAccessCompany(User, employee.Branch))
            {
                return Forbid("You are not authorized to view employee details from this company branch.");
            }

            var assets = employee.IssuedAssets.Select(a => new
            {
                assetId = a.ITAssetId,
                assetType = a.AssetType.ToString(),
                description = a.Description ?? a.ITAsset?.Model ?? "",
                assetTag = a.AssetTag ?? a.ITAsset?.AssetTag ?? "",
                serialNumber = a.SerialNumber ?? a.ITAsset?.SerialNumber ?? "",
                brandModel = a.BrandModel ?? a.ITAsset?.Model ?? ""
            }).ToList();

            return Ok(new
            {
                fullName = employee.FullName,
                employeeId = employee.EmployeeID,
                department = employee.Department,
                branch = employee.Branch,
                email = employee.EmailAddress,
                contactNumber = employee.ContactNumber,
                hodEmail = await IT_Form.Helpers.UserHelper.GetLatestHODEmailByCompanyAsync(_context, employee.Branch, employee.Department) ?? employee.HODEmail,
                assets = assets
            });
        }

        [Authorize(Policy = "RequireHR")]
        [HttpGet("hods-by-company")]
        /// <summary>
        /// Gets the data required by the GetHodsByCompany operation.
        /// </summary>
        public async Task<IActionResult> GetHodsByCompany([FromQuery] string? company)
        {
            if (string.IsNullOrWhiteSpace(company))
            {
                return Ok(Array.Empty<object>());
            }

            // Enforce company scope: an HR user must not enumerate HOD records
            // belonging to a company they are not assigned to.
            if (!CompanyScopeHelper.CanAccessCompany(User, company))
            {
                return Forbid("You are not authorized to view HODs for this company.");
            }

            var normalizedCompany = CompanyHelper.Normalize(company).ToLower();
            var hods = await _context.Users
                .Where(u => u.IsActive && u.UserTitles.Any(t => t.Title == "HOD"))
                .Where(u => u.UserCompanies.Any(c =>
                    c.CompanyName != null &&
                    (c.CompanyName.ToLower() == normalizedCompany ||
                     c.CompanyName.ToLower().StartsWith(normalizedCompany + " ("))))
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    fullName = u.FullName
                })
                .ToListAsync();

            return Ok(hods);
        }

        private static readonly string[] EmployeeIdHeaders =
        {
            "Employee ID",
            "Employee Number",
            "Employee No",
            "Emp ID",
            "Staff ID"
        };

        private static readonly string[] FullNameHeaders = { "Full Name", "Employee Name", "Name" };
        private static readonly string[] PositionHeaders = { "Position", "Designation", "Job Title" };
        private static readonly string[] EmailHeaders = { "Email", "Email Address", "Work Email" };
        private static readonly string[] DateHireHeaders =
        {
            "Date Hire",
            "Date Hired",
            "Hire Date",
            "Join Date"
        };

        private static readonly string[] DeskNumberHeaders = { "Desk Number", "Desk No", "Desk" };
        private static readonly string[] DepartmentHeaders = { "Department", "Dept" };
        private static readonly string[] ContactNumberHeaders =
        {
            "Contact Number",
            "Contact No",
            "Phone Number",
            "Phone"
        };

        private static readonly string[] ReportingManagerHeaders =
        {
            "Reporting Manager",
            "Reporting Manager Email",
            "Manager Email",
            "HOD Email"
        };

        private static readonly string[] CompanyHeaders = { "Company", "Branch", "Company Branch" };

        /// <summary>
        /// Builds the value returned by the BuildColumnMap operation.
        /// </summary>
        private static Dictionary<string, int> BuildColumnMap(IXLRangeRow? headerRow)
        {
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (headerRow == null)
            {
                return columnMap;
            }

            foreach (var cell in headerRow.CellsUsed())
            {
                var normalizedHeader = NormalizeHeader(cell.GetString());
                if (!string.IsNullOrWhiteSpace(normalizedHeader))
                {
                    columnMap[normalizedHeader] = cell.Address.ColumnNumber;
                }
            }

            return columnMap;
        }

        /// <summary>
        /// Gets the data required by the GetColumnNumber operation.
        /// </summary>
        private static int GetColumnNumber(IReadOnlyDictionary<string, int> columnMap, IEnumerable<string> headerAliases, int fallbackColumn)
        {
            foreach (var alias in headerAliases)
            {
                var normalizedAlias = NormalizeHeader(alias);
                if (columnMap.TryGetValue(normalizedAlias, out var columnNumber))
                {
                    return columnNumber;
                }
            }

            return fallbackColumn;
        }

        /// <summary>
        /// Gets the data required by the GetCellValue operation.
        /// </summary>
        private static string? GetCellValue(IXLRangeRow row, IReadOnlyDictionary<string, int> columnMap, IEnumerable<string> headerAliases, int fallbackColumn)
        {
            var columnNumber = GetColumnNumber(columnMap, headerAliases, fallbackColumn);
            if (columnNumber <= 0)
            {
                return null;
            }

            return row.Cell(columnNumber).GetString().Trim();
        }

        /// <summary>
        /// Runs the NormalizeHeader operation.
        /// </summary>
        private static string NormalizeHeader(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return string.Empty;
            }

            return new string(header
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }
    }
}

