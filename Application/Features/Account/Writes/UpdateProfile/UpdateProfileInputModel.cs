using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Account.Writes.UpdateProfile;

public sealed class UpdateProfileInputModel
{
    public string? JobTitle { get; set; }
    public IFormFile? SignatureImageFile { get; set; }
    public bool RemoveSignatureImage { get; set; }
}
