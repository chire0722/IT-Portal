// Project comment: GetOnboardingItSetupQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItSetup
{
    /// <summary>
    /// Handles the GetOnboardingItSetupQuery query workflow.
    /// </summary>
    public sealed class GetOnboardingItSetupQueryHandler : IRequestHandler<GetOnboardingItSetupQuery, ProcessResult<OnboardingViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnboardingWizardService _onboardingWizardService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOnboardingItSetupQueryHandler(
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
        public async Task<ProcessResult<OnboardingViewModel>> Handle(GetOnboardingItSetupQuery request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return ProcessResult<OnboardingViewModel>.NotFound();
            }

            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, onboardingRequest.Branch))
            {
                return ProcessResult<OnboardingViewModel>.Forbidden();
            }

            if (!onboardingRequest.CanStartItSetup())
            {
                return ProcessResult<OnboardingViewModel>.Failure("This request is not awaiting IT setup.", "Details", onboardingRequest.Id);
            }

            var session = await _onboardingWizardService.RestoreItSetupSessionAsync(request.Id);
            if (session == null)
            {
                return ProcessResult<OnboardingViewModel>.NotFound();
            }

            return ProcessResult<OnboardingViewModel>.Success(session, requestId: onboardingRequest.Id);
        }
    }
}
