// Project comment: AddCurrentEmployeeToEmailGroupCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup
{
    /// <summary>
    /// Carries data needed to execute the AddCurrentEmployeeToEmailGroup command.
    /// </summary>
    public sealed record AddCurrentEmployeeToEmailGroupCommand(
        int EmailGroupId,
        int OnboardingRequestId) : IRequest<AddCurrentEmployeeToEmailGroupResult>;
}
