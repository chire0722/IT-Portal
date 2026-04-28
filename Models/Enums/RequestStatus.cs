// Project comment: RequestStatus.cs is part of the IT Form application source code.

namespace IT_Form.Models.Enums
{
    /// <summary>
    /// Lists the supported RequestStatus values used by the application.
    /// </summary>
    public enum RequestStatus
    {
        Draft = 0,
        PendingApproval = 1,
        PendingHRApproval = 8,
        PendingHODApproval = 9,
        PendingITApproval = 3,
        Approved = 4,
        Completed = 5,
        Rejected = 6,
        PendingEmployeeSign = 7
    }
}
