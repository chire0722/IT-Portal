using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Account.Writes.UpdateProfile;

public sealed record UpdateProfileCommand(
    string UserId,
    string? JobTitle,
    IFormFile? SignatureImageFile,
    bool RemoveSignatureImage) : IRequest<Unit>;
