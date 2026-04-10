using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2
{
    public sealed class CompleteOnboardingHrStep2CommandHandler
        : IRequestHandler<CompleteOnboardingHrStep2Command, CompleteOnboardingHrStep2Result>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnboardingWizardService _onboardingWizardService;
        private readonly IEmailBackgroundQueue _emailBackgroundQueue;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public CompleteOnboardingHrStep2CommandHandler(
            ApplicationDbContext context,
            IOnboardingWizardService onboardingWizardService,
            IEmailBackgroundQueue emailBackgroundQueue,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _onboardingWizardService = onboardingWizardService;
            _emailBackgroundQueue = emailBackgroundQueue;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<CompleteOnboardingHrStep2Result> Handle(CompleteOnboardingHrStep2Command request, CancellationToken cancellationToken)
        {
            var session = request.Session;

            if (session.Id == 0 && request.Model.Id > 0)
            {
                session.Id = request.Model.Id;
            }

            if (string.IsNullOrEmpty(session.FullName) && session.Id > 0)
            {
                var backupRequest = await _context.OnboardingRequests
                    .Include(r => r.IssuedAssets)
                    .Include(r => r.FolderPermissions)
                    .Include(r => r.RequestSoftware)
                    .Include(r => r.RequestEmailGroups)
                    .Include(r => r.RequestSharedMailboxes)
                    .FirstOrDefaultAsync(r => r.Id == session.Id, cancellationToken);

                if (backupRequest != null)
                {
                    _onboardingWizardService.MapRequestToSession(backupRequest, session);
                }
            }

            if (string.IsNullOrWhiteSpace(session.FullName) ||
                string.IsNullOrWhiteSpace(session.EmployeeID) ||
                string.IsNullOrWhiteSpace(session.Designation) ||
                string.IsNullOrWhiteSpace(session.Department) ||
                string.IsNullOrWhiteSpace(session.Branch) ||
                string.IsNullOrWhiteSpace(session.HODEmail))
            {
                return new CompleteOnboardingHrStep2Result(
                    CompleteOnboardingHrStep2Status.MissingStep1,
                    session.Id,
                    "Step 1 data is missing or session expired. Please complete Employee Details again.");
            }

            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, session.Branch))
            {
                return new CompleteOnboardingHrStep2Result(
                    CompleteOnboardingHrStep2Status.Forbidden,
                    session.Id,
                    "You can only complete onboarding requests for your assigned company scope.");
            }

            session.IssuedAssets = request.Model.IssuedAssets?
                .Where(a => a.IsChecked && (a.AssetType != AssetType.Others || !string.IsNullOrWhiteSpace(a.Description)))
                .ToList() ?? new List<AssetItemViewModel>();
            session.AssetRemarks = request.Model.AssetRemarks;
            _onboardingWizardService.ClearHrRestrictedAccessFields(session);

            var isNewRequest = session.Id <= 0;
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
                    return new CompleteOnboardingHrStep2Result(
                        CompleteOnboardingHrStep2Status.NotFound,
                        session.Id,
                        "Request not found.");
                }

                if (!existingRequest.CanBeEdited())
                {
                    return new CompleteOnboardingHrStep2Result(
                        CompleteOnboardingHrStep2Status.Locked,
                        existingRequest.Id,
                        "Cannot modify a request that has already been signed or completed.");
                }

                if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, existingRequest.Branch))
                {
                    return new CompleteOnboardingHrStep2Result(
                        CompleteOnboardingHrStep2Status.Forbidden,
                        existingRequest.Id,
                        "You can only modify onboarding requests within your assigned company scope.");
                }

                onboardingRequest = existingRequest;
                onboardingRequest.RestoreDraft();
                _onboardingWizardService.MapSessionToRequest(session, onboardingRequest);
            }
            else
            {
                onboardingRequest = new OnboardingRequest
                {
                    CreatedByUserId = request.CreatedByUserName
                };
                _onboardingWizardService.MapSessionToRequest(session, onboardingRequest);
                _context.OnboardingRequests.Add(onboardingRequest);
            }

            onboardingRequest.UpdatedAt = DateTime.UtcNow;

            if (isNewRequest &&
                !string.IsNullOrWhiteSpace(onboardingRequest.EmailAddress) &&
                !string.IsNullOrWhiteSpace(onboardingRequest.FullName))
            {
                await _emailBackgroundQueue.QueueEmailAsync(
                    new WelcomeEmailMessage(
                        onboardingRequest.EmailAddress,
                        onboardingRequest.FullName),
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            session.Id = onboardingRequest.Id;

            return new CompleteOnboardingHrStep2Result(
                CompleteOnboardingHrStep2Status.Completed,
                onboardingRequest.Id,
                "Onboarding form completed. Please sign below to submit to IT.");
        }
    }
}
