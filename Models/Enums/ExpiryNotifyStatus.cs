// Project comment: ExpiryNotifyStatus.cs is part of the IT Form application source code.

namespace IT_Form.Models.Enums
{
    /// <summary>
    /// Lists the supported ExpiryNotifyStatus values used by the application.
    /// </summary>
    public enum ExpiryNotifyStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2,
        DispatchPending = 3
    }
}
