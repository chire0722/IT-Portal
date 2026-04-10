using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDetails
{
    public sealed record GetOnboardingDetailsQuery(int Id) : IRequest<GetOnboardingDetailsResult>;
}
