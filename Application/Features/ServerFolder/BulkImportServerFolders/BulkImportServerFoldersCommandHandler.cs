// Project comment: BulkImportServerFoldersCommandHandler.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ServerFolder.BulkImportServerFolders
{
    /// <summary>
    /// Handles the BulkImportServerFoldersCommand command workflow.
    /// </summary>
    public sealed class BulkImportServerFoldersCommandHandler : IRequestHandler<BulkImportServerFoldersCommand, BulkImportServerFoldersResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public BulkImportServerFoldersCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<BulkImportServerFoldersResult> Handle(BulkImportServerFoldersCommand request, CancellationToken cancellationToken)
        {
            if (!FileUploadHelper.IsValidExcelFile(request.File, out string errorMessage))
            {
                return new BulkImportServerFoldersResult(false, null, null, errorMessage);
            }

            try
            {
                using var stream = new MemoryStream();
                await request.File.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
                using var workbook = new XLWorkbook(stream);

                var worksheet = workbook.Worksheet(1);
                var rangeUsed = worksheet.RangeUsed();
                var rows = rangeUsed != null ? rangeUsed.RowsUsed().Skip(1) : Enumerable.Empty<IXLRangeRow>();

                int successCount = 0;
                int skipCount = 0;
                int errorCount = 0;
                var importErrors = new List<string>();

                var allCompanies = await _context.Companies.ToListAsync(cancellationToken);
                var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
                var accessibleCompanyIds = scopedCompanies.Count > 0
                    ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                    : null;
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                foreach (var row in rows)
                {
                    var folderName = row.Cell(1).GetValue<string>()?.Trim();
                    if (string.IsNullOrEmpty(folderName))
                    {
                        continue;
                    }

                    var companyNamesText = row.Cell(2).GetValue<string>()?.Trim();
                    var companyNames = CompanyImportMatchHelper.SplitCompanyReferences(companyNamesText);

                    var companyIds = new List<int>();
                    var unmatchedCompanies = new List<string>();
                    foreach (var cn in companyNames)
                    {
                        if (cn.Equals("All Company", StringComparison.OrdinalIgnoreCase))
                        {
                            if (accessibleCompanyIds is { Count: > 0 })
                            {
                                unmatchedCompanies.Add(cn);
                            }

                            continue;
                        }

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
                        importErrors.Add(accessibleCompanyIds is { Count: > 0 } && unmatchedCompanies.Contains("All Company", StringComparer.OrdinalIgnoreCase)
                            ? $"Row {row.RowNumber()}: Company-scoped users cannot import server folders for all companies."
                            : $"Row {row.RowNumber()}: Unknown company reference(s): {string.Join(", ", unmatchedCompanies)}.");
                        continue;
                    }

                    var distinctCompanyIds = companyIds.Distinct().ToList();
                    if (accessibleCompanyIds is { Count: > 0 } &&
                        (distinctCompanyIds.Count == 0 || !distinctCompanyIds.All(accessibleCompanyIds.Contains)))
                    {
                        errorCount++;
                        importErrors.Add($"Row {row.RowNumber()}: You can only import server folders for your assigned company scope.");
                        continue;
                    }

                    if (await _context.ServerFolders.AnyAsync(f => f.FolderName == folderName, cancellationToken))
                    {
                        skipCount++;
                        continue;
                    }

                    try
                    {
                        var isActiveStr = row.Cell(3).GetValue<string>()?.Trim().ToLowerInvariant();
                        bool isActive = string.IsNullOrEmpty(isActiveStr) || isActiveStr == "yes" || isActiveStr == "true";

                        var folder = new IT_Form.Models.ServerFolder
                        {
                            FolderName = folderName,
                            IsActive = isActive
                        };

                        _context.ServerFolders.Add(folder);
                        await _context.SaveChangesAsync(cancellationToken);

                        foreach (var cid in distinctCompanyIds)
                        {
                            _context.ServerFolderCompanies.Add(new ServerFolderCompany { ServerFolderId = folder.Id, CompanyId = cid });
                        }
                        successCount++;
                    }
                    catch
                    {
                        errorCount++;
                    }
                }

                if (successCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                var successMessage = successCount > 0 ? $"Successfully imported {successCount} folders." : null;
                var infoMessage = skipCount > 0 ? $"{skipCount} folders were skipped as they already exist." : null;
                var importErrorMessage = errorCount > 0
                    ? $"Failed to import {errorCount} row(s): " + string.Join(", ", importErrors.Take(5))
                    : null;

                return new BulkImportServerFoldersResult(true, successMessage, infoMessage, importErrorMessage);
            }
            catch (Exception ex)
            {
                return new BulkImportServerFoldersResult(false, null, null, "Error processing file: " + ex.Message);
            }
        }
    }
}
