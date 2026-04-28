// Project comment: ProcessOffboardingRejectionCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Carries data needed to execute the ProcessOffboardingRejection command.
    /// </summary>
    public sealed record ProcessOffboardingRejectionCommand(int Id, string Reason) : IRequest<CommandResponse>;
}
