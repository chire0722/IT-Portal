using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingStep1
{
    public sealed class SaveOnboardingStep1CommandHandler
        : IRequestHandler<SaveOnboardingStep1Command, SaveOnboardingStep1Result>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnboardingWizardService _onboardingWizardService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public SaveOnboardingStep1CommandHandler(
            ApplicationDbContext context,
            IOnboardingWizardService onboardingWizardService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _onboardingWizardService = onboardingWizardService;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<SaveOnboardingStep1Result> Handle(SaveOnboardingStep1Command request, CancellationToken cancellationToken)
        {
            var session = request.Session;
            var createdByUserName = _currentUserAccessor.UserName;
            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, request.Model.Branch))
            {
                return new SaveOnboardingStep1Result(
                    SaveOnboardingStep1Status.Forbidden,
                    request.Model.Id,
                    "You can only create onboarding requests for your assigned company scope.");
            }

            if (session.Id == 0 && request.Model.Id > 0)
            {
                session.Id = request.Model.Id;
            }

            if (session.Id > 0)
            {
                var existing = await _context.OnboardingRequests.FindAsync(new object[] { session.Id }, cancellationToken);
                if (existing != null &&
                    (existing.Status == RequestStatus.PendingEmployeeSign || existing.Status == RequestStatus.Completed))
                {
                    return new SaveOnboardingStep1Result(
                        SaveOnboardingStep1Status.Locked,
                        existing.Id,
                        "Cannot modify a request that has already been signed or completed.");
                }
                if (existing != null && !CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, existing.Branch))
                {
                    return new SaveOnboardingStep1Result(
                        SaveOnboardingStep1Status.Forbidden,
                        existing.Id,
                        "You can only modify onboarding requests within your assigned company scope.");
                }
            }

            _onboardingWizardService.MapStep1ToSession(request.Model, session);
            _onboardingWizardService.ClearHrRestrictedAccessFields(session);

            OnboardingRequest draftRequest;
            if (session.Id > 0)
            {
                var existingDraft = await _context.OnboardingRequests
                    .Include(r => r.IssuedAssets)
                    .Include(r => r.FolderPermissions)
                    .Include(r => r.RequestSoftware)
                    .Include(r => r.RequestEmailGroups)
                    .Include(r => r.RequestSharedMailboxes)
                    .FirstOrDefaultAsync(r => r.Id == session.Id, cancellationToken);

                if (existingDraft == null)
                {
                    draftRequest = new OnboardingRequest
                    {
                        CreatedByUserId = createdByUserName,
                        Status = RequestStatus.Draft
                    };
                    _context.OnboardingRequests.Add(draftRequest);
                }
                else
                {
                    draftRequest = existingDraft;
                }
            }
            else
            {
                draftRequest = new OnboardingRequest
                {
                    CreatedByUserId = createdByUserName,
                    Status = RequestStatus.Draft
                };
                _context.OnboardingRequests.Add(draftRequest);
            }

            draftRequest.Status = RequestStatus.Draft;
            draftRequest.UpdatedAt = DateTime.UtcNow;
            _onboardingWizardService.MapSessionToRequest(session, draftRequest);
            await _context.SaveChangesAsync(cancellationToken);

            session.Id = draftRequest.Id;
            session.CurrentStep = 2;

            return new SaveOnboardingStep1Result(
                SaveOnboardingStep1Status.ReadyForStep2,
                draftRequest.Id,
                "Step 1 saved.");
        }
    }
}
