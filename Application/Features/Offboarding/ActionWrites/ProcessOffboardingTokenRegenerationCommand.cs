// Project comment: ProcessOffboardingTokenRegenerationCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Carries data needed to execute the ProcessOffboardingTokenRegeneration command.
    /// </summary>
    public sealed record ProcessOffboardingTokenRegenerationCommand(int Id) : IRequest<CommandResponse>;
}
