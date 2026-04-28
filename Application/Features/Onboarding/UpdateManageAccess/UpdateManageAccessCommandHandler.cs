// Project comment: UpdateManageAccessCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    /// <summary>
    /// Handles the UpdateManageAccessCommand command workflow.
    /// </summary>
    public sealed class UpdateManageAccessCommandHandler
        : IRequestHandler<UpdateManageAccessCommand, UpdateManageAccessResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentEmployeeAccessService _accessService;

        public UpdateManageAccessCommandHandler(
            ApplicationDbContext context,
            ICurrentEmployeeAccessService accessService)
        {
            _context = context;
            _accessService = accessService;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<UpdateManageAccessResult> Handle(UpdateManageAccessCommand request, CancellationToken cancellationToken)
        {
            var context = await _accessService.GetEmployeeContextAsync(request.OnboardingId, cancellationToken);
            if (context == null)
            {
                var exists = await _context.OnboardingRequests.AnyAsync(r => r.Id == request.OnboardingId, cancellationToken);
                return new UpdateManageAccessResult(
                    exists ? UpdateManageAccessStatus.InvalidState : UpdateManageAccessStatus.NotFound,
                    request.OnboardingId,
                    exists
                        ? "Access assignment is only available for current employees with completed onboarding records."
                        : "Request not found.");
            }

            var onboardingRequest = await _context.OnboardingRequests
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .Include(r => r.FolderPermissions)
                .FirstOrDefaultAsync(r => r.Id == request.OnboardingId, cancellationToken);

            if (onboardingRequest == null)
            {
                return new UpdateManageAccessResult(
                    UpdateManageAccessStatus.NotFound,
                    request.OnboardingId,
                    "Request not found.");
            }

            var accessCatalog = await _accessService.GetAccessCatalogAsync(
                context.CompanyId,
                onboardingRequest.RequestEmailGroups.Where(g => g.EmailGroupId > 0).Select(g => g.EmailGroupId),
                onboardingRequest.RequestSharedMailboxes.Where(m => m.SharedMailboxId > 0).Select(m => m.SharedMailboxId),
                onboardingRequest.FolderPermissions,
                cancellationToken);

            var selections = UpdateManageAccessSupport.BuildSelections(accessCatalog, request, onboardingRequest.Id);
            UpdateManageAccessSupport.ReplaceAccessCollections(_context, onboardingRequest, selections);
            UpdateManageAccessSupport.ApplyOtherAccessFields(onboardingRequest, request);

            _accessService.MapLegacyFlags(selections.EmailGroups, selections.SharedMailboxes, onboardingRequest);

            onboardingRequest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateManageAccessResult(
                UpdateManageAccessStatus.Updated,
                onboardingRequest.Id,
                $"Access assignment updated for {onboardingRequest.FullName}.");
        }
    }
}
