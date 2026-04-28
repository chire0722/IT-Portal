// Project comment: AccountProfilePageViewModel.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Account.Queries.GetProfilePage;

/// <summary>
/// Provides view data for the AccountProfilePage screen or form.
/// </summary>
public sealed class AccountProfilePageViewModel
{
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? JobTitle { get; init; }
    public string? EmailSignatureImage { get; init; }
    public string? Signature { get; init; }
    public List<string> Titles { get; init; } = [];
}
