// Project comment: UpdateProfileCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Helpers;

namespace IT_Form.Application.Features.Account.Writes.UpdateProfile;

/// <summary>
/// Validates input for the UpdateProfileCommand request.
/// </summary>
public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/gif"];

    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.JobTitle)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.JobTitle));

        RuleFor(x => x.SignatureImageFile)
            .Must(file => file == null || file.Length <= 512_000)
            .WithMessage("Signature image must be under 500 KB.");

        RuleFor(x => x.SignatureImageFile)
            .Must(file => file == null || AllowedContentTypes.Contains(file.ContentType?.ToLowerInvariant() ?? string.Empty))
            .WithMessage("Only PNG, JPG, and GIF images are allowed.");

        RuleFor(x => x.SignatureImageFile)
            .Must(file => file == null || file.Length == 0 || UploadFileSignatureValidator.HasAllowedSignature(file, ".png", ".jpg", ".jpeg", ".gif"))
            .WithMessage("The signature image content does not match its file type.");
    }
}
