// Project comment: ImportSharedMailboxesCommandHandler.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes
{
    /// <summary>
    /// Handles the ImportSharedMailboxesCommand command workflow.
    /// </summary>
    public sealed class ImportSharedMailboxesCommandHandler : IRequestHandler<ImportSharedMailboxesCommand, ImportSharedMailboxesResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ImportSharedMailboxesCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ImportSharedMailboxesResult> Handle(ImportSharedMailboxesCommand request, CancellationToken cancellationToken)
        {
            if (!FileUploadHelper.IsValidExcelFile(request.File, out string errorMessage))
            {
                return new ImportSharedMailboxesResult(false, string.Empty, errorMessage);
            }

            int added = 0;
            int skipped = 0;
            int errorCount = 0;
            var errorMessages = new List<string>();

            using var stream = request.File.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);
            var usedRange = ws.RangeUsed();
            if (usedRange == null)
            {
                return new ImportSharedMailboxesResult(false, string.Empty, "The Excel file appears to be empty.");
            }
            var rows = usedRange.RowsUsed().Skip(1);

            var allCompanies = await _context.Companies.ToListAsync(cancellationToken);
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            foreach (var row in rows)
            {
                var name = row.Cell(1).GetString().Trim();
                var address = row.Cell(2).GetString().Trim();
                if (string.IsNullOrEmpty(name))
                {
                    skipped++;
                    continue;
                }

                var companyNamesText = row.Cell(3).GetString().Trim();
                var companyNames = CompanyImportMatchHelper.SplitCompanyReferences(companyNamesText);

                var companyIds = new List<int>();
                var unmatchedCompanies = new List<string>();
                foreach (var cn in companyNames)
                {
                    var company = CompanyImportMatchHelper.FindCompanyByReference(allCompanies, cn);
                    if (company != null)
                    {
                        companyIds.Add(company.Id);
                    }
                    else
                    {
                        unmatchedCompanies.Add(cn);
                    }
                }

                if (unmatchedCompanies.Count > 0)
                {
                    errorCount++;
                    errorMessages.Add($"Row {row.RowNumber()}: Unknown company reference(s): {string.Join(", ", unmatchedCompanies)}.");
                    continue;
                }

                var distinctCompanyIds = companyIds.Distinct().ToList();
                if (accessibleCompanyIds is { Count: > 0 } &&
                    (distinctCompanyIds.Count == 0 || !distinctCompanyIds.All(accessibleCompanyIds.Contains)))
                {
                    errorCount++;
                    errorMessages.Add($"Row {row.RowNumber()}: You can only import shared mailboxes for your assigned company scope.");
                    continue;
                }

                bool isActive = row.Cell(4).GetString().Trim().ToLowerInvariant() != "false";

                if (!_context.SharedMailboxes.Any(m => m.MailboxAddress == address))
                {
                    var newMailbox = new IT_Form.Models.SharedMailbox
                    {
                        MailboxName = name,
                        MailboxAddress = address,
                        IsActive = isActive
                    };
                    _context.SharedMailboxes.Add(newMailbox);
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var cid in distinctCompanyIds)
                    {
                        _context.SharedMailboxCompanies.Add(new SharedMailboxCompany { SharedMailboxId = newMailbox.Id, CompanyId = cid });
                    }
                    added++;
                }
                else
                {
                    skipped++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var successMessage = $"Import complete: {added} added, {skipped} skipped.";
            var importErrorMessage = errorCount > 0
                ? $"Failed to import {errorCount} row(s): {string.Join(", ", errorMessages.Take(5))}"
                : null;

            return new ImportSharedMailboxesResult(true, successMessage, importErrorMessage);
        }
    }
}
