// Project comment: QuickAllocateSubscriptionSeatResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat
{
    /// <summary>
    /// Lists the supported QuickAllocateSubscriptionSeatStatus values used by the application.
    /// </summary>
    public enum QuickAllocateSubscriptionSeatStatus
    {
        Allocated,
        NotFound,
        NotSubscription
    }

    /// <summary>
    /// Represents the result returned by the QuickAllocateSubscriptionSeat workflow.
    /// </summary>
    public sealed record QuickAllocateSubscriptionSeatResult(
        QuickAllocateSubscriptionSeatStatus Status,
        string Message);
}
