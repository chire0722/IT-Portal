using MediatR;

namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    public sealed record SignOnboardingRequestCommand(
        int Id,
        string SignatureType,
        string SignatureData) : IRequest<SignOnboardingRequestResult>;
}
