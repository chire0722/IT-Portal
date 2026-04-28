// Project comment: OnboardingSessionAccessor.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Services
{
    /// <summary>
    /// Defines the OnboardingSessionAccessor class used by the IT Form application.
    /// </summary>
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

        /// <summary>
        /// Gets the data required by the Get operation.
        /// </summary>
        public OnboardingViewModel Get() =>
            _onboardingWizardService.GetFromSession(GetSession());

        /// <summary>
        /// Saves data for the Save operation.
        /// </summary>
        public void Save(OnboardingViewModel model) =>
            _onboardingWizardService.SaveToSession(GetSession(), model);

        /// <summary>
        /// Runs the Remove operation.
        /// </summary>
        public void Remove() => GetSession().Remove("OnboardingWizard");

        /// <summary>
        /// Creates data for the CreateNewAsync operation.
        /// </summary>
        public Task<OnboardingViewModel> CreateNewAsync(CancellationToken cancellationToken = default) =>
            _onboardingWizardService.CreateNewSessionAsync(cancellationToken);

        /// <summary>
        /// Runs the RestoreDraftAsync operation.
        /// </summary>
        public Task<OnboardingViewModel?> RestoreDraftAsync(int requestId, CancellationToken cancellationToken = default) =>
            _onboardingWizardService.RestoreDraftSessionAsync(requestId, cancellationToken);

        /// <summary>
        /// Runs the RestoreItSetupAsync operation.
        /// </summary>
        public Task<OnboardingViewModel?> RestoreItSetupAsync(int requestId, CancellationToken cancellationToken = default) =>
            _onboardingWizardService.RestoreItSetupSessionAsync(requestId, cancellationToken);

        /// <summary>
        /// Runs the PrepareDraftAsync operation.
        /// </summary>
        public Task<OnboardingViewModel> PrepareDraftAsync(OnboardingViewModel currentSession, OnboardingViewModel postedModel, CancellationToken cancellationToken = default) =>
            _onboardingWizardService.PrepareDraftSessionAsync(currentSession, postedModel, cancellationToken);

        /// <summary>
        /// Runs the PrepareStep2 operation.
        /// </summary>
        public OnboardingViewModel PrepareStep2(OnboardingViewModel model) =>
            _onboardingWizardService.PrepareStep2Session(model);

        /// <summary>
        /// Gets the data required by the GetSession operation.
        /// </summary>
        private ISession GetSession() =>
            _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("HTTP session is not available.");
    }
}
