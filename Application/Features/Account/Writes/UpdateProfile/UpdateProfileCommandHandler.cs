// Project comment: UpdateProfileCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Validation;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Services;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.Account.Writes.UpdateProfile;

/// <summary>
/// Handles the UpdateProfileCommand command workflow.
/// </summary>
public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IFileMalwareScanner? _malwareScanner;

    public UpdateProfileCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IFileMalwareScanner? malwareScanner = null)
    {
        _userManager = userManager;
        _context = context;
        _malwareScanner = malwareScanner;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);
        }

        request.Adapt(user);

        if (request.RemoveSignatureImage)
        {
            user.EmailSignatureImage = null;
        }

        if (request.SignatureImageFile != null && request.SignatureImageFile.Length > 0)
        {
            await MalwareScanHelper.EnsureCleanAsync(
                request.SignatureImageFile,
                _malwareScanner,
                "Email signature image",
                cancellationToken);

            var reencoded = await ImageReencodingHelper.ReencodeAsync(
                request.SignatureImageFile,
                cancellationToken);
            var base64 = Convert.ToBase64String(reencoded.Bytes);
            user.EmailSignatureImage = $"data:{reencoded.ContentType};base64,{base64}";
        }

        (await _userManager.UpdateAsync(user)).EnsureSucceeded();

        var auditTime = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            CreatedAt = auditTime,
            Timestamp = auditTime,
            Action = "Update Profile",
            PerformedBy = user.FullName ?? user.UserName ?? "Unknown",
            EntityName = "User Account",
            EntityId = user.Id,
            Details = $"User updated profile details (Job Title: {user.JobTitle ?? "(empty)"}, Signature Image: {(user.EmailSignatureImage != null ? "set" : "removed/empty")})",
            IsSuccess = true
        });
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

