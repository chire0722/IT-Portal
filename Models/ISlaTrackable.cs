using IT_Form.Models.Enums;

namespace IT_Form.Models
{
    public interface ISlaTrackable
    {
        int Id { get; }
        RequestStatus Status { get; }
        SlaNotifyStatus SlaNotifyStatus { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
