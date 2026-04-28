// Project comment: RegenerateOnboardingTokenCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken
{
    /// <summary>
    /// Carries data needed to execute the RegenerateOnboardingToken command.
    /// </summary>
    public sealed record RegenerateOnboardingTokenCommand(int Id) : IRequest<ProcessResult<object>>;
}
