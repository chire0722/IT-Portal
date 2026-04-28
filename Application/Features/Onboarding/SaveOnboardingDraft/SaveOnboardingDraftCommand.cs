// Project comment: SaveOnboardingDraftCommand.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingDraft
{
    /// <summary>
    /// Carries data needed to execute the SaveOnboardingDraft command.
    /// </summary>
    public sealed record SaveOnboardingDraftCommand(OnboardingViewModel Model, string? CreatedByUserName)
        : IRequest<SaveOnboardingDraftResult>;
}

