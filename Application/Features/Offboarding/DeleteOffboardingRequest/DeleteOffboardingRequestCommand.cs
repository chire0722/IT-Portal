// Project comment: DeleteOffboardingRequestCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest
{
    /// <summary>
    /// Carries data needed to execute the DeleteOffboardingRequest command.
    /// </summary>
    public sealed record DeleteOffboardingRequestCommand(int Id) : IRequest<CommandResponse>;
}
