// Project comment: UpdateProfileInputModel.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Account.Writes.UpdateProfile;

/// <summary>
/// Defines the UpdateProfileInputModel class used by the IT Form application.
/// </summary>
public sealed class UpdateProfileInputModel
{
    public string? JobTitle { get; set; }
    public IFormFile? SignatureImageFile { get; set; }
    public bool RemoveSignatureImage { get; set; }
}
