using IT_Form.Data;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
    public sealed class UpdateOnboardingItStep4CommandHandler
        : IRequestHandler<UpdateOnboardingItStep4Command, UpdateOnboardingItStep4Result>
    {
        private readonly ApplicationDbContext _context;

        public UpdateOnboardingItStep4CommandHandler(ApplicationDbContext context)
        {
            _context = context;
        }

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
