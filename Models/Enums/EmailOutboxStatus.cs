namespace IT_Form.Models.Enums;

public enum EmailOutboxStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3,
    DeadLetter = 4
}
