// Project comment: ProcessOnboardingRejectionCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    /// <summary>
    /// Carries data needed to execute the ProcessOnboardingRejection command.
    /// </summary>
    public sealed record ProcessOnboardingRejectionCommand(int Id, string Reason) : IRequest<CommandResponse>;
}
