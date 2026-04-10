namespace IT_Form.Application.Features.Account.Queries.GetLoginPageBranding;

public sealed class LoginPageBrandingOptions
{
    public string CompanyName { get; set; } = "YTC";
    public string SystemName { get; set; } = "IT Portal";
    public string Subtitle { get; set; } = "IT Asset & Access Management System";
    public string? BackgroundImagePath { get; set; }
    public string? SystemVersion { get; set; }
}
