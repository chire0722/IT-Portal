// Project comment: RejectOnboardingRequestCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.RejectOnboardingRequest
{
    /// <summary>
    /// Carries data needed to execute the RejectOnboardingRequest command.
    /// </summary>
    public sealed record RejectOnboardingRequestCommand(int Id, string Reason) : IRequest<ProcessResult<object>>;
}
