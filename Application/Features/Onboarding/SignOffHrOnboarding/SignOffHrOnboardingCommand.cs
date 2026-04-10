using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.SignOffHrOnboarding
{
    public sealed record SignOffHrOnboardingCommand(
        int Id,
        string SignatureBase64) : IRequest<ProcessResult<object>>;
}
