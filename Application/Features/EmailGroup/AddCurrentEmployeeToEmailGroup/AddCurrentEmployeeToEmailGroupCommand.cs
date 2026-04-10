using MediatR;

namespace IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup
{
    public sealed record AddCurrentEmployeeToEmailGroupCommand(
        int EmailGroupId,
        int OnboardingRequestId) : IRequest<AddCurrentEmployeeToEmailGroupResult>;
}
