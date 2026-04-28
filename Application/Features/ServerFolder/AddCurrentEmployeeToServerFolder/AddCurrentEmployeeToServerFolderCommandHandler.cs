// Project comment: AddCurrentEmployeeToServerFolderCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder
{
    /// <summary>
    /// Handles the AddCurrentEmployeeToServerFolderCommand command workflow.
    /// </summary>
    public sealed class AddCurrentEmployeeToServerFolderCommandHandler : IRequestHandler<AddCurrentEmployeeToServerFolderCommand, AddCurrentEmployeeToServerFolderResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public AddCurrentEmployeeToServerFolderCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<AddCurrentEmployeeToServerFolderResult> Handle(AddCurrentEmployeeToServerFolderCommand request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var folder = await _context.ServerFolders
                .AsNoTracking()
                .Where(f => f.Id == request.ServerFolderId)
                .Select(f => new
                {
                    Folder = f,
                    CompanyIds = f.ServerFolderCompanies.Select(link => link.CompanyId).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (folder == null)
            {
                return new AddCurrentEmployeeToServerFolderResult(AddCurrentEmployeeToServerFolderStatus.FolderNotFound, "Server folder not found.");
            }

            if (accessibleCompanyIds is { Count: > 0 } && !folder.CompanyIds.All(accessibleCompanyIds.Contains))
            {
                return new AddCurrentEmployeeToServerFolderResult(AddCurrentEmployeeToServerFolderStatus.Forbidden, "You can only manage server folders within your company scope.");
            }

            var employee = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.OnboardingRequestId, cancellationToken);
            if (employee == null || (employee.Status != RequestStatus.Completed && employee.Status != RequestStatus.Approved))
            {
                return new AddCurrentEmployeeToServerFolderResult(AddCurrentEmployeeToServerFolderStatus.InvalidEmployee, "Selected employee is not a current employee record.");
            }

            if (!ItAdminCompanyScopeSupport.CanAccessCompany(_currentUserAccessor.User, employee.Branch))
            {
                return new AddCurrentEmployeeToServerFolderResult(AddCurrentEmployeeToServerFolderStatus.Forbidden, "You can only add employees from your own company scope.");
            }

            var canRead = request.CanRead || request.CanModify;
            var canModify = request.CanModify;
            if (!canRead && !canModify)
            {
                canRead = true;
            }

            var existing = await _context.FolderPermissions.FirstOrDefaultAsync(f =>
                f.OnboardingRequestId == request.OnboardingRequestId &&
                f.FolderName == folder.Folder.FolderName, cancellationToken);

            if (existing != null)
            {
                existing.CanRead = existing.CanRead || canRead;
                existing.CanModify = existing.CanModify || canModify;
                employee.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                return new AddCurrentEmployeeToServerFolderResult(
                    AddCurrentEmployeeToServerFolderStatus.Updated,
                    $"Updated folder permission for {employee.FullName}.");
            }

            _context.FolderPermissions.Add(new FolderPermission
            {
                OnboardingRequestId = request.OnboardingRequestId,
                FolderName = folder.Folder.FolderName,
                CanRead = canRead,
                CanModify = canModify
            });

            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new AddCurrentEmployeeToServerFolderResult(
                AddCurrentEmployeeToServerFolderStatus.Added,
                $"Added {employee.FullName} to {folder.Folder.FolderName}.");
        }
    }
}
