// Project comment: ISlaTrackable.cs is part of the IT Form application source code.

using IT_Form.Models.Enums;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the contract implemented by ISlaTrackable services.
    /// </summary>
    public interface ISlaTrackable
    {
        int Id { get; }
        RequestStatus Status { get; }
        SlaNotifyStatus SlaNotifyStatus { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
