// Project comment: ImportStandardSoftwareCommandHandler.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Data;
using IT_Form.Application.Features.StandardSoftware;
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

        public ImportStandardSoftwareCommandHandler(ApplicationDbContext context)
        {
            _context = context;
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

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var rows = usedRange.RowsUsed().Skip(1);
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
                            ContractLimit = quantity
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
