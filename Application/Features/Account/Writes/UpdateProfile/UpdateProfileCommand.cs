// Project comment: UpdateProfileCommand.cs is part of the IT Form application source code.

using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Account.Writes.UpdateProfile;

/// <summary>
/// Carries data needed to execute the UpdateProfile command.
/// </summary>
public sealed record UpdateProfileCommand(
    string UserId,
    string? JobTitle,
    IFormFile? SignatureImageFile,
    bool RemoveSignatureImage) : IRequest<Unit>;
