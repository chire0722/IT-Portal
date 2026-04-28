// Project comment: UpdateOnboardingItStep4CommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
    /// <summary>
    /// Handles the UpdateOnboardingItStep4Command command workflow.
    /// </summary>
    public sealed class UpdateOnboardingItStep4CommandHandler
        : IRequestHandler<UpdateOnboardingItStep4Command, UpdateOnboardingItStep4Result>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public UpdateOnboardingItStep4CommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<UpdateOnboardingItStep4Result> Handle(UpdateOnboardingItStep4Command request, CancellationToken cancellationToken)
        {
            var session = request.Session;

            var onboardingRequest = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == session.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return new UpdateOnboardingItStep4Result(
                    UpdateOnboardingItStep4Status.NotFound,
                    session.Id,
                    "Request not found.");
            }

            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, onboardingRequest.Branch))
            {
                return new UpdateOnboardingItStep4Result(
                    UpdateOnboardingItStep4Status.Forbidden,
                    onboardingRequest.Id,
                    "You can only access IT setup for requests in your assigned company scope.");
            }

            if (onboardingRequest.Status != RequestStatus.PendingITApproval)
            {
                return new UpdateOnboardingItStep4Result(
                    UpdateOnboardingItStep4Status.Locked,
                    onboardingRequest.Id,
                    "IT Setup steps are locked.");
            }

            session.FolderPermissions = request.Model.FolderPermissions ?? new List<FolderPermissionViewModel>();
            session.FolderRemarks = request.Model.FolderRemarks;
            session.OtherFolderDetails = request.Model.OtherFolderDetails;
            session.CurrentStep = 5;

            if (request.IsDraft)
            {
                await OnboardingItSetupPersistence.SaveDraftAsync(_context, onboardingRequest, session, cancellationToken);
                return new UpdateOnboardingItStep4Result(
                    UpdateOnboardingItStep4Status.DraftSaved,
                    onboardingRequest.Id,
                    "Draft saved successfully.");
            }

            return new UpdateOnboardingItStep4Result(
                UpdateOnboardingItStep4Status.ReadyForStep5,
                onboardingRequest.Id,
                "Step 4 saved.");
        }
    }
}
