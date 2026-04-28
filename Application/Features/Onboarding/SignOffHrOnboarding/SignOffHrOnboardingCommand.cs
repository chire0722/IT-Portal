// Project comment: SignOffHrOnboardingCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.SignOffHrOnboarding
{
    /// <summary>
    /// Carries data needed to execute the SignOffHrOnboarding command.
    /// </summary>
    public sealed record SignOffHrOnboardingCommand(
        int Id,
        string SignatureBase64) : IRequest<ProcessResult<object>>;
}
