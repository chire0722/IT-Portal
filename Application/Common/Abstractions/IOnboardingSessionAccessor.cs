using IT_Form.ViewModels;

namespace IT_Form.Application.Common.Abstractions
{
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
