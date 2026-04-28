// Project comment: ImportDepartmentsCommandHandler.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Department.ImportDepartments
{
    /// <summary>
    /// Handles the ImportDepartmentsCommand command workflow.
    /// </summary>
    public sealed class ImportDepartmentsCommandHandler : IRequestHandler<ImportDepartmentsCommand, ImportDepartmentsResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ImportDepartmentsCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ImportDepartmentsResult> Handle(ImportDepartmentsCommand request, CancellationToken cancellationToken)
        {
            if (!FileUploadHelper.IsValidExcelFile(request.File, out string errorMessage))
            {
                return new ImportDepartmentsResult(false, string.Empty, errorMessage);
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
                return new ImportDepartmentsResult(false, string.Empty, "The Excel file appears to be empty.");
            }

            var usesDescriptionColumn = string.Equals(
                ws.Cell(1, 2).GetString().Trim(),
                "Description",
                StringComparison.OrdinalIgnoreCase);

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
                if (string.IsNullOrEmpty(name))
                {
                    skipped++;
                    continue;
                }

                var description = usesDescriptionColumn
                    ? row.Cell(2).GetString().Trim()
                    : null;

                var companyNamesText = usesDescriptionColumn
                    ? row.Cell(3).GetString().Trim()
                    : row.Cell(2).GetString().Trim();

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
                    errorMessages.Add($"Row {row.RowNumber()}: You can only import departments for your assigned company scope.");
                    continue;
                }

                bool isActive = true;
                var activeCell = (usesDescriptionColumn ? row.Cell(4) : row.Cell(3))
                    .GetString()
                    .Trim()
                    .ToLowerInvariant();
                if (activeCell is "false" or "0")
                {
                    isActive = false;
                }

                var existing = await _context.Departments
                    .Include(d => d.DepartmentCompanies)
                    .FirstOrDefaultAsync(d => d.Name == name, cancellationToken);

                if (existing == null)
                {
                    var newDept = new IT_Form.Models.Department
                    {
                        Name = name,
                        Description = string.IsNullOrWhiteSpace(description) ? null : description,
                        IsActive = isActive
                    };
                    _context.Departments.Add(newDept);
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var cid in distinctCompanyIds)
                    {
                        _context.DepartmentCompanies.Add(new DepartmentCompany { DepartmentId = newDept.Id, CompanyId = cid });
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

            return new ImportDepartmentsResult(true, successMessage, importErrorMessage);
        }
    }
}
