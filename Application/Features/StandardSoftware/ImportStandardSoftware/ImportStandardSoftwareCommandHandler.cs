// Project comment: ImportStandardSoftwareCommandHandler.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Data;
using IT_Form.Application.Features.StandardSoftware;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware
{
    /// <summary>
    /// Handles the ImportStandardSoftwareCommand command workflow.
    /// </summary>
    public sealed class ImportStandardSoftwareCommandHandler : IRequestHandler<ImportStandardSoftwareCommand, ImportStandardSoftwareResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuotaControl _quotaControl;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ImportStandardSoftwareCommandHandler(ApplicationDbContext context, IQuotaControl quotaControl, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _quotaControl = quotaControl;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ImportStandardSoftwareResult> Handle(ImportStandardSoftwareCommand request, CancellationToken cancellationToken)
        {
            if (!FileUploadHelper.IsValidExcelFile(request.File, out string errorMessage))
            {
                return new ImportStandardSoftwareResult(false, null, errorMessage);
            }

            int softwareAdded = 0;
            int softwareSkipped = 0;
            int versionsAdded = 0;
            int keysAdded = 0;
            int rowErrorCount = 0;
            var rowErrors = new List<string>();

            using var stream = request.File.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);
            var usedRange = ws.RangeUsed();
            if (usedRange == null)
            {
                return new ImportStandardSoftwareResult(false, null, "The Excel file appears to be empty.");
            }
            if (usedRange.RowCount() > 5001 || usedRange.ColumnCount() > 20)
            {
                return new ImportStandardSoftwareResult(false, null, "Software import is limited to 5,000 data rows and 20 columns.");
            }

            // Scoped IT users import new license versions only into companies they administer.
            // A global IT/Super Admin identity returns null and retains legacy global-import behavior.
            var accessibleCompanyIds = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(
                _context,
                _currentUserAccessor.User,
                cancellationToken);

            if (accessibleCompanyIds is { Count: 0 })
            {
                return new ImportStandardSoftwareResult(false, null, "Your account is not assigned to an active company scope.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var rows = usedRange.RowsUsed().Skip(1);
            var rowList = rows.ToList();
            var candidateSoftware = rowList
                .Select(row => row.Cell(1).GetString().Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var existingSoftware = await _context.StandardSoftware
                .Where(software => candidateSoftware.Contains(software.SoftwareName))
                .Select(software => software.SoftwareName)
                .ToListAsync(cancellationToken);
            var newSoftwareCount = candidateSoftware.Count(name => !existingSoftware.Contains(name, StringComparer.OrdinalIgnoreCase));
            if (newSoftwareCount > 0)
            {
                await _quotaControl.EnsureAvailableAsync(QuotaControl.Software, newSoftwareCount, _currentUserAccessor.User, cancellationToken);
            }

            var candidateVersionCount = rowList.Count(row => !string.IsNullOrWhiteSpace(row.Cell(4).GetString().Trim()));
            if (candidateVersionCount > 0)
            {
                await _quotaControl.EnsureAvailableAsync(QuotaControl.SoftwareVersions, candidateVersionCount, _currentUserAccessor.User, cancellationToken);
            }

            rows = rowList;
            foreach (var row in rows)
            {
                var softwareName = row.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(softwareName))
                {
                    continue;
                }

                bool isMandatory = row.Cell(2).GetString().Trim().ToLowerInvariant() == "true";
                bool isActive = row.Cell(3).GetString().Trim().ToLowerInvariant() != "false";
                var versionName = row.Cell(4).GetString().Trim();
                var licenseKey = row.Cell(5).GetString().Trim();
                var licenseType = row.Cell(6).GetString().Trim();
                var quantityStr = row.Cell(8).GetString().Trim();
                bool isFree = row.Cell(9).GetString().Trim().ToLowerInvariant() == "true";

                var software = await _context.StandardSoftware
                    .Include(s => s.Licenses)
                        .ThenInclude(l => l.Keys)
                    .FirstOrDefaultAsync(s => s.SoftwareName == softwareName, cancellationToken);

                if (software == null)
                {
                    software = new IT_Form.Models.StandardSoftware
                    {
                        SoftwareName = softwareName,
                        IsMandatory = isMandatory,
                        IsActive = isActive
                    };
                    _context.StandardSoftware.Add(software);
                    await _context.SaveChangesAsync(cancellationToken);
                    softwareAdded++;
                }
                else
                {
                    softwareSkipped++;
                }

                if (!string.IsNullOrEmpty(versionName))
                {
                    var existingVersion = software.Licenses.FirstOrDefault(l => l.SoftwareName == versionName);

                    if (existingVersion == null)
                    {
                        bool isSub = licenseType.Equals("Subscription", StringComparison.OrdinalIgnoreCase);
                        if (!ExcelImportDateHelper.TryReadOptionalDate(row.Cell(7), out var expiry, out var expiryError))
                        {
                            rowErrorCount++;
                            rowErrors.Add($"Row {row.RowNumber()}: {expiryError}");
                            continue;
                        }

                        if (isSub && !expiry.HasValue)
                        {
                            rowErrorCount++;
                            rowErrors.Add($"Row {row.RowNumber()}: Expiry Date is required for subscription rows and must use {ExcelImportDateHelper.PreferredDateFormat}.");
                            continue;
                        }

                        int.TryParse(quantityStr, out var quantity);
                        existingVersion = new IT_Form.Models.SoftwareLicense
                        {
                            StandardSoftwareId = software.Id,
                            SoftwareName = versionName,
                            IsSubscription = isSub,
                            IsFree = isFree,
                            ExpiryDate = expiry,
                            ContractLimit = quantity,
                            SoftwareLicenseCompanies = accessibleCompanyIds == null
                                ? []
                                : accessibleCompanyIds.Select(companyId => new IT_Form.Models.SoftwareLicenseCompany
                                {
                                    CompanyId = companyId
                                }).ToList()
                        };
                        SoftwareLicenseSeatSyncSupport.NormalizeLicenseSettings(existingVersion);
                        _context.SoftwareLicenses.Add(existingVersion);
                        await _context.SaveChangesAsync(cancellationToken);
                        await SoftwareLicenseSeatSyncSupport.SyncAutoGeneratedSeatsAsync(_context, existingVersion, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                        software.Licenses.Add(existingVersion);
                        versionsAdded++;
                    }

                    if (!string.IsNullOrEmpty(licenseKey))
                    {
                        if (existingVersion.Id > 0 &&
                            !await StandardSoftwareLicenseScopeSupport.CanAccessAsync(
                                _context,
                                _currentUserAccessor.User,
                                existingVersion.Id,
                                cancellationToken))
                        {
                            rowErrorCount++;
                            rowErrors.Add($"Row {row.RowNumber()}: The existing license version is outside your company scope; its key was not changed.");
                            continue;
                        }

                        bool keyExists = existingVersion.Keys.Any(k => k.KeyValue == licenseKey);
                        if (!keyExists)
                        {
                            var newKey = new IT_Form.Models.SoftwareKey
                            {
                                SoftwareLicenseId = existingVersion.Id,
                                KeyValue = licenseKey,
                                IsAssigned = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.SoftwareKeys.Add(newKey);
                            existingVersion.Keys.Add(newKey);
                            keysAdded++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var successMessage = $"Import complete: {softwareAdded} software added ({softwareSkipped} existing), {versionsAdded} versions added, {keysAdded} keys added.";
            var importErrorMessage = rowErrorCount > 0
                ? $"Skipped {rowErrorCount} row(s): {string.Join(", ", rowErrors.Take(5))}"
                : null;

            return new ImportStandardSoftwareResult(true, successMessage, importErrorMessage);
        }
    }
}
