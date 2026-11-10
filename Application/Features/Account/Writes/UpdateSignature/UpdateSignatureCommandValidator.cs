// Project comment: UpdateSignatureCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Helpers;

namespace IT_Form.Application.Features.Account.Writes.UpdateSignature;

/// <summary>
/// Validates input for the UpdateSignatureCommand request.
/// </summary>
public sealed class UpdateSignatureCommandValidator : AbstractValidator<UpdateSignatureCommand>
{
    public UpdateSignatureCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.SignatureData)
            .Must(signature => string.IsNullOrWhiteSpace(signature) || ImageUploadHelper.TryDecodeRasterImageDataUrl(signature, out _, out _))
            .WithMessage("Signature must be a valid PNG, JPG, GIF, or WebP image under 1 MB.");
    }
}
