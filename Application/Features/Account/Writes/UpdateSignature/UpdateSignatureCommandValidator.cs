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
            .Must(signature => string.IsNullOrWhiteSpace(signature) || SignatureDataUriValidator.IsValid(signature))
            .WithMessage("Signature must be a valid PNG image data URL within the allowed size and dimensions.");

        RuleFor(x => x.SignatureData)
            .MaximumLength(5_000_000)
            .When(x => !string.IsNullOrWhiteSpace(x.SignatureData));
    }
}
