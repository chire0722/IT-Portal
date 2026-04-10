namespace IT_Form.Models.Enums
{
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
