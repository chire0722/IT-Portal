// Project comment: SaveOnboardingDraftCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingDraft
{
    /// <summary>
    /// Handles the SaveOnboardingDraftCommand command workflow.
    /// </summary>
    public sealed class SaveOnboardingDraftCommandHandler
        : IRequestHandler<SaveOnboardingDraftCommand, SaveOnboardingDraftResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnboardingWizardService _onboardingWizardService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public SaveOnboardingDraftCommandHandler(
            ApplicationDbContext context,
            IOnboardingWizardService onboardingWizardService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _onboardingWizardService = onboardingWizardService;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<SaveOnboardingDraftResult> Handle(SaveOnboardingDraftCommand request, CancellationToken cancellationToken)
        {
            var session = request.Model;
            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, session.Branch))
            {
                return new SaveOnboardingDraftResult(
                    SaveOnboardingDraftStatus.Forbidden,
                    session.Id,
                    "You can only save onboarding drafts for your assigned company scope.");
            }

            OnboardingRequest onboardingRequest;
            if (session.Id > 0)
            {
                var existingRequest = await _context.OnboardingRequests
                    .Include(r => r.IssuedAssets)
                    .Include(r => r.FolderPermissions)
                    .Include(r => r.RequestSoftware)
                    .Include(r => r.RequestEmailGroups)
                    .Include(r => r.RequestSharedMailboxes)
                    .FirstOrDefaultAsync(r => r.Id == session.Id, cancellationToken);

                if (existingRequest == null)
                {
                    return new SaveOnboardingDraftResult(
                        SaveOnboardingDraftStatus.NotFound,
                        session.Id,
                        "Request not found.");
                }

                if (existingRequest.Status == RequestStatus.PendingEmployeeSign || existingRequest.Status == RequestStatus.Completed)
                {
                    return new SaveOnboardingDraftResult(
                        SaveOnboardingDraftStatus.InvalidState,
                        existingRequest.Id,
                        "Cannot modify a request that has already been signed or completed.");
                }

                if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, existingRequest.Branch))
                {
                    return new SaveOnboardingDraftResult(
                        SaveOnboardingDraftStatus.Forbidden,
                        existingRequest.Id,
                        "You can only modify onboarding drafts within your assigned company scope.");
                }

                onboardingRequest = existingRequest;
            }
            else
            {
                onboardingRequest = new OnboardingRequest
                {
                    CreatedByUserId = request.CreatedByUserName,
                    Status = RequestStatus.Draft
                };
                _context.OnboardingRequests.Add(onboardingRequest);
            }

            onboardingRequest.Status = RequestStatus.Draft;
            onboardingRequest.UpdatedAt = DateTime.UtcNow;
            _onboardingWizardService.MapSessionToRequest(session, onboardingRequest);

            await _context.SaveChangesAsync(cancellationToken);

            return new SaveOnboardingDraftResult(
                SaveOnboardingDraftStatus.Saved,
                onboardingRequest.Id,
                "Draft saved successfully.");
        }
    }
}
