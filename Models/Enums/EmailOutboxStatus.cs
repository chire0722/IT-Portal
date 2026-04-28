// Project comment: EmailOutboxStatus.cs is part of the IT Form application source code.

namespace IT_Form.Models.Enums;

/// <summary>
/// Lists the supported EmailOutboxStatus values used by the application.
/// </summary>
public enum EmailOutboxStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3,
    DeadLetter = 4
}
