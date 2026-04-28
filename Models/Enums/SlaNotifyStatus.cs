// Project comment: SlaNotifyStatus.cs is part of the IT Form application source code.

// SlaNotifyStatus.cs
namespace IT_Form.Models.Enums
{
    /// <summary>
    /// Lists the supported SlaNotifyStatus values used by the application.
    /// </summary>
    public enum SlaNotifyStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2,
        Tier1Sent = 3,
        Tier2Sent = 4,
        PrimaryDispatchPending = 5,
        Tier1DispatchPending = 6,
        Tier2DispatchPending = 7
    }
}
