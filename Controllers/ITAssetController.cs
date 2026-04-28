// Project comment: ITAssetController.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Form.Data;
using IT_Form.Application.Features.ITAsset.CreateItAsset;
using IT_Form.Application.Features.ITAsset.EditItAsset;
using IT_Form.Application.Common.Support;
using IT_Form.Models;
using IT_Form.Models.Enums;
using System.Linq;
using System.Threading.Tasks;

using IT_Form.Models.ViewModels;
using ClosedXML.Excel;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediatR;
using System.Security.Claims;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Handles web requests for the ITAsset area.
    /// </summary>
    public class ITAssetController : AppControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISender _sender;

        public ITAssetController(ApplicationDbContext context, ISender sender)
        {
            _context = context;
            _sender = sender;
        }

        // GET: ITAsset
        /// <summary>
        /// Runs the Index operation.
        /// </summary>
        public async Task<IActionResult> Index(string searchString, AssetType? assetType, AssetStatus? status, int? companyId = null)
        {
            var companyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, User, CancellationToken.None);
            var selectedCompany = companyId.HasValue
                ? companyOptions.FirstOrDefault(company => company.Id == companyId.Value)
                : null;
            if (companyId.HasValue && selectedCompany == null)
            {
                return Forbid();
            }

            var assetsQuery = ApplyAssetCompanyScope(_context.ITAssets.AsQueryable());

            if (selectedCompany != null)
            {
                assetsQuery = CompanyScopeHelper.ApplyCompanyScope(
                    assetsQuery,
                    [selectedCompany.DisplayName],
                    asset => asset.Company);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                assetsQuery = assetsQuery.Where(a => a.AssetTag.ToLower().Contains(lowerSearch) ||
                                           (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(lowerSearch)) ||
                                           (a.Model != null && a.Model.ToLower().Contains(lowerSearch)));
            }

            if (assetType.HasValue)
            {
                assetsQuery = assetsQuery.Where(a => a.AssetType == assetType.Value);
            }

            if (status.HasValue)
            {
                assetsQuery = assetsQuery.Where(a => a.Status == status.Value);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["AssetTypeFilter"] = assetType;
            ViewData["StatusFilter"] = status;
            ViewData["CompanyFilter"] = companyId;

            // Calculate Analytics
            var today = DateTime.Today;
            var thirtyDaysFromNow = today.AddDays(30);

            var viewModel = new AssetAnalyticsViewModel
            {
                TotalAssets = await assetsQuery.CountAsync(),
                IssuedAssets = await assetsQuery.CountAsync(a => a.Status == AssetStatus.Issued),
                UnderWarrantyCount = await assetsQuery.CountAsync(a => a.WarrantyExpiry != null && a.WarrantyExpiry >= today),
                ExpiredWarrantyCount = await assetsQuery.CountAsync(a => a.WarrantyExpiry != null && a.WarrantyExpiry < today),
                ExpiringSoonCount = await assetsQuery.CountAsync(a => a.WarrantyExpiry != null && a.WarrantyExpiry >= today && a.WarrantyExpiry <= thirtyDaysFromNow),
                Assets = await assetsQuery.Include(a => a.IssuedHistory).ThenInclude(ih => ih.OnboardingRequest).OrderBy(a => a.AssetTag).ToListAsync(),
                SelectedCompanyId = companyId,
                CompanyOptions = companyOptions
            };

            // Populate user list for the Device Verification Link Generator
            var activeUsers = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserCompanies)
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(User);
            ViewBag.UserList = scopedCompanies.Count == 0
                ? activeUsers
                : activeUsers
                    .Where(user => CompanyScopeHelper.HasAnyAllowedCompany(scopedCompanies, user.UserCompanies.Select(company => company.CompanyName)))
                    .ToList();

            return View(viewModel);
        }

        // GET: ITAsset/ExportCsv
        /// <summary>
        /// Runs the ExportCsv operation.
        /// </summary>
        public async Task<IActionResult> ExportCsv(string searchString, AssetType? assetType, AssetStatus? status, int? companyId = null)
        {
            var companyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, User, CancellationToken.None);
            var selectedCompany = companyId.HasValue
                ? companyOptions.FirstOrDefault(company => company.Id == companyId.Value)
                : null;
            if (companyId.HasValue && selectedCompany == null)
            {
                return Forbid();
            }

            var assets = ApplyAssetCompanyScope(_context.ITAssets.AsQueryable());

            if (selectedCompany != null)
            {
                assets = CompanyScopeHelper.ApplyCompanyScope(
                    assets,
                    [selectedCompany.DisplayName],
                    asset => asset.Company);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                assets = assets.Where(a => a.AssetTag.Contains(searchString) ||
                                           (a.SerialNumber != null && a.SerialNumber.Contains(searchString)) ||
                                           (a.Model != null && a.Model.Contains(searchString)));
            }

            if (assetType.HasValue)
            {
                assets = assets.Where(a => a.AssetType == assetType.Value);
            }

            if (status.HasValue)
            {
                assets = assets.Where(a => a.Status == status.Value);
            }

            var list = await assets.Include(a => a.IssuedHistory).ThenInclude(ih => ih.OnboardingRequest).OrderBy(a => a.AssetTag).ToListAsync();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Asset Tag,Type,Model,Serial Number,Status,Assigned To,Purchase Date,Warranty Expiry");

            foreach (var a in list)
            {
                var tag = a.AssetTag?.Replace("\"", "\"\"") ?? "";
                var typeStr = a.AssetType.ToString();
                var model = a.Model?.Replace("\"", "\"\"") ?? "";
                var sn = a.SerialNumber?.Replace("\"", "\"\"") ?? "";
                var statusStr = a.Status.ToString();

                var assignedTo = "";
                if (a.Status == AssetStatus.Issued && a.IssuedHistory.Any())
                {
                    var latest = a.IssuedHistory.OrderByDescending(ih => ih.Id).FirstOrDefault();
                    if (latest != null)
                    {
                        if (latest.OnboardingRequest != null)
                        {
                            assignedTo = latest.OnboardingRequest.FullName?.Replace("\"", "\"\"") ?? "";
                        }
                        else if (!string.IsNullOrEmpty(latest.CustomEmployeeName))
                        {
                            assignedTo = latest.CustomEmployeeName.Replace("\"", "\"\"") + " (Manual)";
                        }
                    }
                }

                var purchase = a.PurchaseDate?.ToString("yyyy-MM-dd") ?? "";
                var warranty = a.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "";

                builder.AppendLine($"\"{tag}\",\"{typeStr}\",\"{model}\",\"{sn}\",\"{statusStr}\",\"{assignedTo}\",\"{purchase}\",\"{warranty}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var resultData = new byte[bom.Length + bytes.Length];
            System.Buffer.BlockCopy(bom, 0, resultData, 0, bom.Length);
            System.Buffer.BlockCopy(bytes, 0, resultData, bom.Length, bytes.Length);

            return File(resultData, "text/csv", $"it_assets_export_{System.DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: ITAsset/Details/5
        /// <summary>
        /// Runs the Details operation.
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var itAsset = await _context.ITAssets
                .Include(a => a.MaintenanceLogs)
                .Include(a => a.IssuedHistory)
                    .ThenInclude(ih => ih.OnboardingRequest)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (itAsset == null) return NotFound();
            if (!ItAdminCompanyScopeSupport.CanAccessCompany(User, itAsset.Company)) return Forbid();

            // Log the Read action
            await _context.LogActionAsync(nameof(ITAsset), id.Value.ToString(), "Read", $"Viewed IT Asset details: {itAsset.AssetTag} ({itAsset.Model})");

            return View(itAsset);
        }

        // GET: ITAsset/Create
        /// <summary>
        /// Creates data for the Create operation.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            await PopulateCreateEditViewBagsAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Creates data for the Create operation.
        /// </summary>
        public async Task<IActionResult> Create([Bind("AssetTag,SerialNumber,AssetType,OtherAssetType,Status,Model,Company,Location,PurchaseDate,WarrantyExpiry,RAM,Storage,WindowsVersion,DDRType,NoExtendWarranty")] ITAsset itAsset, int? AssignedEmployeeId, string? CustomEmployeeName)
        {
            if (itAsset == null) return View(new ITAsset());
            if (!ItAdminCompanyScopeSupport.CanAccessCompany(User, itAsset.Company)) return Forbid();

            if (!ModelState.IsValid)
            {
                await PopulateCreateEditViewBagsAsync(itAsset.Company);
                return View(itAsset);
            }

            return await ExecuteWithValidationResult(
                async () =>
                {
                    var result = await _sender.Send(new CreateItAssetCommand(itAsset, AssignedEmployeeId, CustomEmployeeName));
                    if (result.IsForbidden)
                    {
                        return Forbid();
                    }
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                },
                async () =>
                {
                    await PopulateCreateEditViewBagsAsync(itAsset.Company);
                    return View(itAsset);
                });
        }

        // GET: ITAsset/Edit/5
        /// <summary>
        /// Runs the Edit operation.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var itAsset = await _context.ITAssets.FindAsync(id);
            if (itAsset == null) return NotFound();
            if (!ItAdminCompanyScopeSupport.CanAccessCompany(User, itAsset.Company)) return Forbid();

            if (itAsset.Status == AssetStatus.Issued)
            {
                var latestIssue = await _context.IssuedAssets
                    .Include(ia => ia.OnboardingRequest)
                    .OrderByDescending(ia => ia.Id)
                    .FirstOrDefaultAsync(ia => ia.ITAssetId == id);
                
                if (latestIssue != null)
                {
                    ViewBag.CurrentAssignedEmployeeId = latestIssue.OnboardingRequestId;
                    if (latestIssue.OnboardingRequest != null)
                    {
                        ViewBag.CurrentCustomEmployeeName = $"{latestIssue.OnboardingRequest.FullName} ({latestIssue.OnboardingRequest.Department})";
                    }
                    else
                    {
                        ViewBag.CurrentCustomEmployeeName = latestIssue.CustomEmployeeName;
                    }
                }
            }

            await PopulateCreateEditViewBagsAsync(itAsset.Company);

            return View(itAsset);
        }

        // POST: ITAsset/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the Edit operation.
        /// </summary>
        public async Task<IActionResult> Edit(int id, [Bind("Id,AssetTag,SerialNumber,AssetType,OtherAssetType,Status,Model,Company,Location,PurchaseDate,WarrantyExpiry,RAM,Storage,WindowsVersion,DDRType,NoExtendWarranty")] ITAsset itAsset, int? AssignedEmployeeId, string? CustomEmployeeName)
        {
            if (id != itAsset.Id) return NotFound();
            if (!ItAdminCompanyScopeSupport.CanAccessCompany(User, itAsset.Company)) return Forbid();

            if (!ModelState.IsValid)
            {
                await PopulateCreateEditViewBagsAsync(itAsset.Company);
                return View(itAsset);
            }

            return await ExecuteWithValidationResult(
                async () =>
                {
                    try
                    {
                        var result = await _sender.Send(new EditItAssetCommand(id, itAsset, AssignedEmployeeId, CustomEmployeeName));
                        if (result.IsNotFound)
                        {
                            return NotFound();
                        }
                        if (result.IsForbidden)
                        {
                            return Forbid();
                        }

                        TempData["SuccessMessage"] = result.Message;
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ITAssetExists(itAsset.Id)) return NotFound();
                        throw;
                    }
                },
                async () =>
                {
                    await PopulateCreateEditViewBagsAsync(itAsset.Company);
                    return View(itAsset);
                });
        }

        // GET: ITAsset/Delete/5
        /// <summary>
        /// Deletes data for the Delete operation.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var itAsset = await _context.ITAssets.FirstOrDefaultAsync(m => m.Id == id);
            if (itAsset == null) return NotFound();
            if (!ItAdminCompanyScopeSupport.CanAccessCompany(User, itAsset.Company)) return Forbid();

            return View(itAsset);
        }

        // POST: ITAsset/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Deletes data for the DeleteConfirmed operation.
        /// </summary>
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var itAsset = await _context.ITAssets
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (itAsset == null)
            {
                return RedirectToAction(nameof(Index));
            }
            if (!CompanyScopeHelper.CanAccessCompany(User, itAsset.Company)) return Forbid();

            // Hard delete IT asset row from database.
            // Related maintenance logs are removed by FK cascade; issued history keeps record with ITAssetId set null.
            var affectedRows = await _context.ITAssets
                .IgnoreQueryFilters()
                .Where(a => a.Id == id)
                .ExecuteDeleteAsync();

            if (affectedRows > 0)
            {
                await _context.LogActionAsync(
                    nameof(ITAsset),
                    id.ToString(),
                    "Delete",
                    $"Permanently deleted IT Asset: {itAsset.AssetTag} ({itAsset.Model ?? "N/A"})");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the ToggleNoExtendWarranty operation.
        /// </summary>
        public async Task<IActionResult> ToggleNoExtendWarranty(int id)
        {
            var asset = await _context.ITAssets.FindAsync(id);
            if (asset == null) return NotFound();
            if (!ItAdminCompanyScopeSupport.CanAccessCompany(User, asset.Company)) return Forbid();

            asset.NoExtendWarranty = !asset.NoExtendWarranty;
            if (!asset.NoExtendWarranty)
            {
                // Re-enable reminder cycle when warranty extension planning is resumed.
                asset.WarrantyNotifyStatus = ExpiryNotifyStatus.Pending;
            }
            _context.Update(asset);
            await _context.SaveChangesAsync();

            string status = asset.NoExtendWarranty ? "marked as 'No Extend'" : "unmarked as 'No Extend'";
            await _context.LogActionAsync(nameof(ITAsset), id.ToString(), "Update", $"{status} for asset {asset.AssetTag}");

            TempData["SuccessMessage"] = $"Asset {asset.AssetTag} {status}. Expiry notifications for this item are now {(asset.NoExtendWarranty ? "disabled" : "enabled")}.";

            return RedirectToAction(nameof(Index));
        }

        // GET: ITAsset/DownloadImportTemplate
        /// <summary>
        /// Runs the DownloadImportTemplate operation.
        /// </summary>
        public async Task<IActionResult> DownloadImportTemplate()
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Assets");
                
                // Add Headers
                string[] headers = {
                    "Asset Tag*", "Serial Number", "Asset Type (Dropdown)*", "Status (Dropdown)*", 
                    "Model / Make", "Purchase Date (dd/MM/yyyy)", "Warranty Expiry (dd/MM/yyyy)", 
                    "RAM (e.g. 16GB)", "DDR Type (Dropdown)", "Storage (e.g. 512GB SSD)", 
                    "Windows Version (Dropdown)"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;
                }

                // Setup Categories sheet for Data Validation
                var catSheet = workbook.Worksheets.Add("Categories");
                catSheet.Hide();

                // 1. Asset Type
                var assetTypes = Enum.GetNames(typeof(AssetType)).ToList();
                for (int i = 0; i < assetTypes.Count; i++) catSheet.Cell(i + 1, 1).Value = assetTypes[i];
                var assetTypeRange = catSheet.Range(1, 1, assetTypes.Count, 1);
                workbook.DefinedNames.Add("AssetTypes", assetTypeRange);

                // 2. Status
                var statuses = Enum.GetNames(typeof(AssetStatus)).ToList();
                for (int i = 0; i < statuses.Count; i++) catSheet.Cell(i + 1, 2).Value = statuses[i];
                var statusRange = catSheet.Range(1, 2, statuses.Count, 2);
                workbook.DefinedNames.Add("Statuses", statusRange);

                // 3. DDR Type
                var ddrTypes = new List<string> { "DDR", "DDR2", "DDR3", "DDR4", "DDR5" };
                for (int i = 0; i < ddrTypes.Count; i++) catSheet.Cell(i + 1, 3).Value = ddrTypes[i];
                var ddrRange = catSheet.Range(1, 3, ddrTypes.Count, 3);
                workbook.DefinedNames.Add("DdrTypes", ddrRange);

                // 4. Windows Version
                var winVersions = await _context.SoftwareLicenses
                    .Where(s => s.SystemMappingKey == "OS")
                    .OrderBy(s => s.SoftwareName)
                    .Select(s => s.SoftwareName)
                    .ToListAsync();
                if (!winVersions.Any()) winVersions.Add("N/A");
                for (int i = 0; i < winVersions.Count; i++) catSheet.Cell(i + 1, 4).Value = winVersions[i];
                var winRange = catSheet.Range(1, 4, winVersions.Count, 4);
                workbook.DefinedNames.Add("WinVersions", winRange);

                // Apply Data Validation to Asset sheet (first 100 rows)
                var assetRangeForValidation = worksheet.Range("A2:K100");
                worksheet.Column(3).AsRange().CreateDataValidation().List("=AssetTypes", true);
                worksheet.Column(3).AsRange().GetDataValidation().IgnoreBlanks = true;
                worksheet.Column(3).AsRange().GetDataValidation().ShowErrorMessage = false; // Allow manual entry

                worksheet.Column(4).AsRange().CreateDataValidation().List("=Statuses", true);
                worksheet.Column(4).AsRange().GetDataValidation().ShowErrorMessage = false;

                worksheet.Column(9).AsRange().CreateDataValidation().List("=DdrTypes", true);
                worksheet.Column(9).AsRange().GetDataValidation().ShowErrorMessage = false;

                worksheet.Column(11).AsRange().CreateDataValidation().List("=WinVersions", true);
                worksheet.Column(11).AsRange().GetDataValidation().ShowErrorMessage = false;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "IT_Asset_Import_Template.xlsx");
                }
            }
        }

        // POST: ITAsset/BulkImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the BulkImport operation.
        /// </summary>
        public async Task<IActionResult> BulkImport(IFormFile file)
        {
            if (CompanyScopeHelper.HasScopedCompanies(User))
            {
                TempData["ErrorMessage"] = "Bulk import is disabled for company-scoped IT users because the current template does not carry company ownership.";
                return RedirectToAction(nameof(Index));
            }

            if (!FileUploadHelper.IsValidExcelFile(file, out string errorMessage))
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rangeUsed = worksheet.RangeUsed();
                        var rows = rangeUsed != null ? rangeUsed.RowsUsed().Skip(1) : Enumerable.Empty<IXLRangeRow>();

                        int successCount = 0;
                        int errorCount = 0;
                        var errorMessages = new List<string>();

                        foreach (var row in rows)
                        {
                            var assetTag = row.Cell(1).GetValue<string>()?.Trim();
                            if (string.IsNullOrEmpty(assetTag)) continue;

                            if (await _context.ITAssets.AnyAsync(a => a.AssetTag == assetTag))
                            {
                                errorCount++;
                                errorMessages.Add($"Row {row.RowNumber()}: Asset Tag '{assetTag}' already exists.");
                                continue;
                            }

                            try
                            {
                                var assetTypeStr = row.Cell(3).GetValue<string>()?.Trim();
                                var statusStr = row.Cell(4).GetValue<string>()?.Trim();

                                AssetType assetType;
                                string? otherAssetType = null;

                                if (!Enum.TryParse<AssetType>(assetTypeStr, out assetType))
                                {
                                    assetType = AssetType.Others;
                                    otherAssetType = assetTypeStr;
                                }
                                else if (assetType == AssetType.Others)
                                {
                                    // If they selected 'Others' in dropdown, it remains 'Others'.
                                }

                                if (!Enum.TryParse<AssetStatus>(statusStr, out var status)) status = AssetStatus.Available;

                                var asset = new ITAsset
                                {
                                    AssetTag = assetTag,
                                    SerialNumber = row.Cell(2).GetValue<string>()?.Trim(),
                                    AssetType = assetType,
                                    OtherAssetType = otherAssetType,
                                    Status = status,
                                    Model = row.Cell(5).GetValue<string>()?.Trim(),
                                    RAM = row.Cell(8).GetValue<string>()?.Trim(),
                                    DDRType = row.Cell(9).GetValue<string>()?.Trim(),
                                    Storage = row.Cell(10).GetValue<string>()?.Trim(),
                                    WindowsVersion = row.Cell(11).GetValue<string>()?.Trim(),
                                };

                                if (!ExcelImportDateHelper.TryReadOptionalDate(row.Cell(6), out var purchaseDate, out var purchaseDateError))
                                {
                                    errorCount++;
                                    errorMessages.Add($"Row {row.RowNumber()}: {purchaseDateError}");
                                    continue;
                                }

                                if (!ExcelImportDateHelper.TryReadOptionalDate(row.Cell(7), out var warrantyDate, out var warrantyDateError))
                                {
                                    errorCount++;
                                    errorMessages.Add($"Row {row.RowNumber()}: {warrantyDateError}");
                                    continue;
                                }

                                asset.PurchaseDate = purchaseDate;
                                asset.WarrantyExpiry = warrantyDate;

                                _context.ITAssets.Add(asset);
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                errorMessages.Add($"Row {row.RowNumber()}: {ex.Message}");
                            }
                        }

                        if (successCount > 0)
                        {
                            await _context.SaveChangesAsync();
                            TempData["SuccessMessage"] = $"Successfully imported {successCount} assets.";
                        }

                        if (errorCount > 0)
                        {
                            TempData["ErrorMessage"] = $"Failed to import {errorCount} rows: " + string.Join(", ", errorMessages.Take(5));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error processing file: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Runs the ITAssetExists operation.
        /// </summary>
        private bool ITAssetExists(int id)
        {
            return _context.ITAssets.Any(e => e.Id == id);
        }

        /// <summary>
        /// Runs the PopulateCreateEditViewBagsAsync operation.
        /// </summary>
        private async Task PopulateCreateEditViewBagsAsync(string? selectedCompany = null)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(User);
            ViewBag.WindowsVersions = await _context.SoftwareLicenses
                .Where(s => s.SystemMappingKey == "OS")
                .OrderBy(s => s.SoftwareName)
                .Select(s => s.SoftwareName)
                .ToListAsync();

            ViewBag.DDRTypes = new List<string> { "DDR", "DDR2", "DDR3", "DDR4", "DDR5" };

            var employeesQuery = _context.OnboardingRequests
                .Where(o => o.Status == RequestStatus.Completed || o.Status == RequestStatus.PendingEmployeeSign || o.Status == RequestStatus.Approved)
                .AsQueryable();
            if (scopedCompanies.Count > 0)
            {
                employeesQuery = CompanyScopeHelper.ApplyOnboardingCompanyScope(employeesQuery, scopedCompanies);
            }

            var employees = await employeesQuery
                .OrderBy(o => o.FullName)
                .Select(o => new { o.Id, Name = $"{o.FullName} ({o.Department})" })
                .ToListAsync();
            ViewBag.Employees = new SelectList(employees, "Id", "Name");

            var companies = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyDisplayNamesAsync(_context, User, CancellationToken.None);
            ViewBag.Companies = new SelectList(companies, selectedCompany);
        }

        /// <summary>
        /// Applies changes for the ApplyAssetCompanyScope operation.
        /// </summary>
        private IQueryable<ITAsset> ApplyAssetCompanyScope(IQueryable<ITAsset> query)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(User);
            if (scopedCompanies.Count == 0)
            {
                return query;
            }

            return CompanyScopeHelper.ApplyCompanyScope(query, scopedCompanies, asset => asset.Company);
        }
    }
}
