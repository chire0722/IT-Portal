// Project comment: ProcessOffboardingCheckerApprovalCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Carries data needed to execute the ProcessOffboardingCheckerApproval command.
    /// </summary>
    public sealed record ProcessOffboardingCheckerApprovalCommand(int Id, string SignatureBase64) : IRequest<CommandResponse>;
}
