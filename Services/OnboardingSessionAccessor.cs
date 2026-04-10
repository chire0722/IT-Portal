using IT_Form.Application.Common.Abstractions;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Services
{
    public sealed class OnboardingSessionAccessor : IOnboardingSessionAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOnboardingWizardService _onboardingWizardService;

        public OnboardingSessionAccessor(
            IHttpContextAccessor httpContextAccessor,
            IOnboardingWizardService onboardingWizardService)
        {
            _httpContextAccessor = httpContextAccessor;
            _onboardingWizardService = onboardingWizardService;
        }

        public OnboardingViewModel Get() =>
            _onboardingWizardService.GetFromSession(GetSession());

        public void Save(OnboardingViewModel model) =>
            _onboardingWizardService.SaveToSession(GetSession(), model);

        public void Remove() => GetSession().Remove("OnboardingWizard");

        public Task<OnboardingViewModel> CreateNewAsync(CancellationToken cancellationToken = default) =>
            _onboardingWizardService.CreateNewSessionAsync(cancellationToken);

        public Task<OnboardingViewModel?> RestoreDraftAsync(int requestId, CancellationToken cancellationToken = default) =>
            _onboardingWizardService.RestoreDraftSessionAsync(requestId, cancellationToken);

        public Task<OnboardingViewModel?> RestoreItSetupAsync(int requestId, CancellationToken cancellationToken = default) =>
            _onboardingWizardService.RestoreItSetupSessionAsync(requestId, cancellationToken);

        public Task<OnboardingViewModel> PrepareDraftAsync(OnboardingViewModel currentSession, OnboardingViewModel postedModel, CancellationToken cancellationToken = default) =>
            _onboardingWizardService.PrepareDraftSessionAsync(currentSession, postedModel, cancellationToken);

        public OnboardingViewModel PrepareStep2(OnboardingViewModel model) =>
            _onboardingWizardService.PrepareStep2Session(model);

        private ISession GetSession() =>
            _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("HTTP session is not available.");
    }
}
