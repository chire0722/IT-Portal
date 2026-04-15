using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup
{
    public sealed class AddCurrentEmployeeToEmailGroupCommandHandler : IRequestHandler<AddCurrentEmployeeToEmailGroupCommand, AddCurrentEmployeeToEmailGroupResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public AddCurrentEmployeeToEmailGroupCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<AddCurrentEmployeeToEmailGroupResult> Handle(AddCurrentEmployeeToEmailGroupCommand request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var group = await _context.EmailGroups
                .AsNoTracking()
                .Where(g => g.Id == request.EmailGroupId)
                .Select(g => new
                {
                    Group = g,
                    CompanyIds = g.EmailGroupCompanies.Select(link => link.CompanyId).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (group == null)
            {
                return new AddCurrentEmployeeToEmailGroupResult(AddCurrentEmployeeToEmailGroupStatus.GroupNotFound, "Email group not found.");
            }

            if (accessibleCompanyIds is { Count: > 0 } && !group.CompanyIds.All(accessibleCompanyIds.Contains))
            {
                return new AddCurrentEmployeeToEmailGroupResult(AddCurrentEmployeeToEmailGroupStatus.Forbidden, "You can only manage email groups within your company scope.");
            }

            var employee = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.OnboardingRequestId, cancellationToken);
            if (employee == null || (employee.Status != RequestStatus.Completed && employee.Status != RequestStatus.Approved))
            {
                return new AddCurrentEmployeeToEmailGroupResult(AddCurrentEmployeeToEmailGroupStatus.InvalidEmployee, "Selected employee is not a current employee record.");
            }

            if (!ItAdminCompanyScopeSupport.CanAccessCompany(_currentUserAccessor.User, employee.Branch))
            {
                return new AddCurrentEmployeeToEmailGroupResult(AddCurrentEmployeeToEmailGroupStatus.Forbidden, "You can only add employees from your own company scope.");
            }

            var exists = await _context.RequestEmailGroups.AnyAsync(x =>
                x.EmailGroupId == request.EmailGroupId && x.OnboardingRequestId == request.OnboardingRequestId, cancellationToken);
            if (exists)
            {
                return new AddCurrentEmployeeToEmailGroupResult(
                    AddCurrentEmployeeToEmailGroupStatus.AlreadyExists,
                    $"{employee.FullName} already has access to {group.Group.GroupName}.");
            }

            _context.RequestEmailGroups.Add(new RequestEmailGroup
            {
                OnboardingRequestId = request.OnboardingRequestId,
                EmailGroupId = group.Group.Id,
                GroupName = group.Group.GroupName,
                GroupEmail = group.Group.GroupEmail
            });

            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new AddCurrentEmployeeToEmailGroupResult(
                AddCurrentEmployeeToEmailGroupStatus.Added,
                $"Added {employee.FullName} to {group.Group.GroupName}.");
        }
    }
}
