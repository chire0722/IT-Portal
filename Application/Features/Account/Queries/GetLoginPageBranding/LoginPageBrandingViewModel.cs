// Project comment: LoginPageBrandingViewModel.cs is part of the IT Form application source code.

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Account.Queries.GetLoginPageBranding;

/// <summary>
/// Provides view data for the LoginPageBranding screen or form.
/// </summary>
public sealed class LoginPageBrandingViewModel : LoginViewModel
{
    public string CompanyName { get; set; } = "YTC";
    public string SystemName { get; set; } = "IT Portal";
    public string Subtitle { get; set; } = "IT Asset & Access Management System";
    public string? LoginLogoPath { get; set; }
    public string? BackgroundImagePath { get; set; }
    public string SystemVersion { get; set; } = string.Empty;
    public string? LockoutEndTimeIso { get; set; }
}
