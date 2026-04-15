using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox
{
    public sealed class AddCurrentEmployeeToSharedMailboxCommandHandler : IRequestHandler<AddCurrentEmployeeToSharedMailboxCommand, AddCurrentEmployeeToSharedMailboxResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public AddCurrentEmployeeToSharedMailboxCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<AddCurrentEmployeeToSharedMailboxResult> Handle(AddCurrentEmployeeToSharedMailboxCommand request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var mailbox = await _context.SharedMailboxes
                .AsNoTracking()
                .Where(m => m.Id == request.SharedMailboxId)
                .Select(m => new
                {
                    Mailbox = m,
                    CompanyIds = m.SharedMailboxCompanies.Select(link => link.CompanyId).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (mailbox == null)
            {
                return new AddCurrentEmployeeToSharedMailboxResult(AddCurrentEmployeeToSharedMailboxStatus.MailboxNotFound, "Shared mailbox not found.");
            }

            if (accessibleCompanyIds is { Count: > 0 } && !mailbox.CompanyIds.All(accessibleCompanyIds.Contains))
            {
                return new AddCurrentEmployeeToSharedMailboxResult(AddCurrentEmployeeToSharedMailboxStatus.Forbidden, "You can only manage shared mailboxes within your company scope.");
            }

            var employee = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.OnboardingRequestId, cancellationToken);
            if (employee == null || (employee.Status != RequestStatus.Completed && employee.Status != RequestStatus.Approved))
            {
                return new AddCurrentEmployeeToSharedMailboxResult(AddCurrentEmployeeToSharedMailboxStatus.InvalidEmployee, "Selected employee is not a current employee record.");
            }

            if (!ItAdminCompanyScopeSupport.CanAccessCompany(_currentUserAccessor.User, employee.Branch))
            {
                return new AddCurrentEmployeeToSharedMailboxResult(AddCurrentEmployeeToSharedMailboxStatus.Forbidden, "You can only add employees from your own company scope.");
            }

            var exists = await _context.RequestSharedMailboxes.AnyAsync(x =>
                x.SharedMailboxId == request.SharedMailboxId && x.OnboardingRequestId == request.OnboardingRequestId, cancellationToken);
            if (exists)
            {
                return new AddCurrentEmployeeToSharedMailboxResult(
                    AddCurrentEmployeeToSharedMailboxStatus.AlreadyExists,
                    $"{employee.FullName} already has access to {mailbox.Mailbox.MailboxName}.");
            }

            _context.RequestSharedMailboxes.Add(new RequestSharedMailbox
            {
                OnboardingRequestId = request.OnboardingRequestId,
                SharedMailboxId = mailbox.Mailbox.Id,
                MailboxName = mailbox.Mailbox.MailboxName,
                MailboxAddress = mailbox.Mailbox.MailboxAddress
            });

            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new AddCurrentEmployeeToSharedMailboxResult(
                AddCurrentEmployeeToSharedMailboxStatus.Added,
                $"Added {employee.FullName} to {mailbox.Mailbox.MailboxName}.");
        }
    }
}
