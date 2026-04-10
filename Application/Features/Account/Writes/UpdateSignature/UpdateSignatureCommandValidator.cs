using FluentValidation;

namespace IT_Form.Application.Features.Account.Writes.UpdateSignature;

public sealed class UpdateSignatureCommandValidator : AbstractValidator<UpdateSignatureCommand>
{
    public UpdateSignatureCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.SignatureData)
            .Must(signature => string.IsNullOrWhiteSpace(signature) || signature.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Signature must be a valid image data URL.");

        RuleFor(x => x.SignatureData)
            .MaximumLength(5_000_000)
            .When(x => !string.IsNullOrWhiteSpace(x.SignatureData));
    }
}
