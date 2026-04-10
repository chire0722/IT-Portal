namespace IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat
{
    public enum QuickAllocateSubscriptionSeatStatus
    {
        Allocated,
        NotFound,
        NotSubscription
    }

    public sealed record QuickAllocateSubscriptionSeatResult(
        QuickAllocateSubscriptionSeatStatus Status,
        string Message);
}
