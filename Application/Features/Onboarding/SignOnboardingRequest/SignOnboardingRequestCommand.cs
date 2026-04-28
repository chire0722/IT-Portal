// Project comment: SignOnboardingRequestCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    /// <summary>
    /// Carries data needed to execute the SignOnboardingRequest command.
    /// </summary>
    public sealed record SignOnboardingRequestCommand(
        int Id,
        string SignatureType,
        string SignatureData) : IRequest<SignOnboardingRequestResult>;
}
