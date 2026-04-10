using IT_Form.Models;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Services
{
    public interface IOnboardingWizardService
    {
        OnboardingViewModel GetFromSession(ISession session);
        void SaveToSession(ISession session, OnboardingViewModel model);
        Task<OnboardingViewModel> CreateNewSessionAsync(CancellationToken cancellationToken = default);
        Task<OnboardingViewModel?> RestoreDraftSessionAsync(int requestId, CancellationToken cancellationToken = default);
        Task<OnboardingViewModel?> RestoreItSetupSessionAsync(int requestId, CancellationToken cancellationToken = default);
        Task<OnboardingViewModel> PrepareDraftSessionAsync(OnboardingViewModel currentSession, OnboardingViewModel postedModel, CancellationToken cancellationToken = default);
        OnboardingViewModel PrepareStep2Session(OnboardingViewModel model);
        Task<OnboardingViewModel> EnsureStep4FolderPermissionsAsync(OnboardingViewModel model, string? branchName, CancellationToken cancellationToken = default);
        void MapRequestToSession(OnboardingRequest request, OnboardingViewModel session);
        void MapStep1ToSession(OnboardingViewModel model, OnboardingViewModel session);
        void MapSessionToRequest(OnboardingViewModel session, OnboardingRequest request);
        void ClearHrRestrictedAccessFields(OnboardingViewModel session);
    }
}
