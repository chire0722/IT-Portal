using IT_Form.Data;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep4
{
    public sealed class GetOnboardingItStep4QueryHandler : IRequestHandler<GetOnboardingItStep4Query, GetOnboardingItStep4Result>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnboardingWizardService _onboardingWizardService;

        public GetOnboardingItStep4QueryHandler(
            ApplicationDbContext context,
            IOnboardingWizardService onboardingWizardService)
        {
            _context = context;
            _onboardingWizardService = onboardingWizardService;
        }

        public async Task<GetOnboardingItStep4Result> Handle(GetOnboardingItStep4Query request, CancellationToken cancellationToken)
        {
            var model = request.SessionModel;
            if (model == null || model.Id == 0)
            {
                return new GetOnboardingItStep4Result(
                    GetOnboardingItStep4Status.SessionExpired,
                    null,
                    "Session expired. Please use 'Continue IT Setup' from the request details page.",
                    0);
            }

            var dbRequest = await _context.OnboardingRequests.FindAsync(new object[] { model.Id }, cancellationToken);
            if (dbRequest == null)
            {
                return new GetOnboardingItStep4Result(GetOnboardingItStep4Status.NotFound, null, null, model.Id);
            }

            if (dbRequest.Status != RequestStatus.PendingITApproval)
            {
                return new GetOnboardingItStep4Result(
                    GetOnboardingItStep4Status.Locked,
                    null,
                    "IT Setup steps are locked.",
                    model.Id);
            }

            model = await _onboardingWizardService.EnsureStep4FolderPermissionsAsync(model, dbRequest.Branch, cancellationToken);

            return new GetOnboardingItStep4Result(GetOnboardingItStep4Status.Ready, model, null, model.Id);
        }
    }
}
