using MediatR;

namespace IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox
{
    public sealed record AddCurrentEmployeeToSharedMailboxCommand(
        int SharedMailboxId,
        int OnboardingRequestId) : IRequest<AddCurrentEmployeeToSharedMailboxResult>;
}
