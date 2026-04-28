// Project comment: RejectOffboardingRequestCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.RejectOffboardingRequest
{
    /// <summary>
    /// Carries data needed to execute the RejectOffboardingRequest command.
    /// </summary>
    public sealed record RejectOffboardingRequestCommand(int Id, string Reason) : IRequest<ProcessResult<object>>;
}
