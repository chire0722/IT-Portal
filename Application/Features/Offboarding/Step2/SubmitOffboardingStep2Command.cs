// Project comment: SubmitOffboardingStep2Command.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    /// <summary>
    /// Carries data needed to execute the SubmitOffboardingStep2 command.
    /// </summary>
    public sealed record SubmitOffboardingStep2Command(int Id, string SaveAction, OffboardingRequest Request) : IRequest<ProcessResult<object>>;
}
