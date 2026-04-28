// Project comment: QuickAllocateSubscriptionSeatCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat
{
    /// <summary>
    /// Carries data needed to execute the QuickAllocateSubscriptionSeat command.
    /// </summary>
    public sealed record QuickAllocateSubscriptionSeatCommand(int SoftwareId) : IRequest<QuickAllocateSubscriptionSeatResult>;
}
