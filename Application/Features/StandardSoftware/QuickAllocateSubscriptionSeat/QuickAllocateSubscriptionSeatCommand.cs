using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat
{
    public sealed record QuickAllocateSubscriptionSeatCommand(int SoftwareId) : IRequest<QuickAllocateSubscriptionSeatResult>;
}
