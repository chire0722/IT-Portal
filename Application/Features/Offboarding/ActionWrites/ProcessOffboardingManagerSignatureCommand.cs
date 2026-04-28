// Project comment: ProcessOffboardingManagerSignatureCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Carries data needed to execute the ProcessOffboardingManagerSignature command.
    /// </summary>
    public sealed record ProcessOffboardingManagerSignatureCommand(int Id, string SignatureBase64) : IRequest<CommandResponse>;
}
