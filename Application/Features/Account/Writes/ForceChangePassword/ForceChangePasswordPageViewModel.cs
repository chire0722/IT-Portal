// Project comment: ForceChangePasswordPageViewModel.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Application.Features.Account.Writes.ForceChangePassword;

/// <summary>
/// Provides view data for the ForceChangePasswordPage screen or form.
/// </summary>
public sealed class ForceChangePasswordPageViewModel
{
    [Display(Name = "Current Password")]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Display(Name = "New Password")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Confirm New Password")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
