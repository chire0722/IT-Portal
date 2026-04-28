// Project comment: IOnboardingSessionAccessor.cs is part of the IT Form application source code.

using IT_Form.ViewModels;

namespace IT_Form.Application.Common.Abstractions
{
    /// <summary>
    /// Defines the contract implemented by IOnboardingSessionAccessor services.
    /// </summary>
    public interface IOnboardingSessionAccessor
    {
        OnboardingViewModel Get();
        void Save(OnboardingViewModel model);
        void Remove();
        Task<OnboardingViewModel> CreateNewAsync(CancellationToken cancellationToken = default);
        Task<OnboardingViewModel?> RestoreDraftAsync(int requestId, CancellationToken cancellationToken = default);
        Task<OnboardingViewModel?> RestoreItSetupAsync(int requestId, CancellationToken cancellationToken = default);
        Task<OnboardingViewModel> PrepareDraftAsync(OnboardingViewModel currentSession, OnboardingViewModel postedModel, CancellationToken cancellationToken = default);
        OnboardingViewModel PrepareStep2(OnboardingViewModel model);
    }
}
