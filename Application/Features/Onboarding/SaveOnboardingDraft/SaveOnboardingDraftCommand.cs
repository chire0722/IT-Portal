using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingDraft
{
    public sealed record SaveOnboardingDraftCommand(OnboardingViewModel Model, string? CreatedByUserName)
        : IRequest<SaveOnboardingDraftResult>;
}

