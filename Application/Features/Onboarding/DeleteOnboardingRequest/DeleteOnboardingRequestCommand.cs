// Project comment: DeleteOnboardingRequestCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest
{
    /// <summary>
    /// Carries data needed to execute the DeleteOnboardingRequest command.
    /// </summary>
    public sealed record DeleteOnboardingRequestCommand(int Id, bool IsAjax) : IRequest<ProcessResult<object>>;
}
