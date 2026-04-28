// Project comment: AddCurrentEmployeeToSharedMailboxCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox
{
    /// <summary>
    /// Carries data needed to execute the AddCurrentEmployeeToSharedMailbox command.
    /// </summary>
    public sealed record AddCurrentEmployeeToSharedMailboxCommand(
        int SharedMailboxId,
        int OnboardingRequestId) : IRequest<AddCurrentEmployeeToSharedMailboxResult>;
}
