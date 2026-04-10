using System.ComponentModel.DataAnnotations;

namespace IT_Form.Application.Features.Account.Writes.ForceChangePassword;

public sealed class ForceChangePasswordPageViewModel
{
    [Display(Name = "Current Password")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Display(Name = "New Password")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Confirm New Password")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
