using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDelete
{
    public sealed record GetOnboardingDeleteQuery(int Id) : IRequest<ProcessResult<OnboardingRequest>>;
}
