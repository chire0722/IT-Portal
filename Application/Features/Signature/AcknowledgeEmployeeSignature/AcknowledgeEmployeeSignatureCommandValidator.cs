// Project comment: AcknowledgeEmployeeSignatureCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Helpers;

namespace IT_Form.Application.Features.Signature.AcknowledgeEmployeeSignature
{
    /// <summary>
    /// Validates input for the AcknowledgeEmployeeSignatureCommand request.
    /// </summary>
    public sealed class AcknowledgeEmployeeSignatureCommandValidator : AbstractValidator<AcknowledgeEmployeeSignatureCommand>
    {
        public AcknowledgeEmployeeSignatureCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEqual(Guid.Empty);

            RuleFor(x => x.Type)
                .Must(type => string.Equals(type, "Onboarding", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(type, "Offboarding", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Unknown request type.");

            RuleFor(x => x.SignatureData)
                .Must(SignatureDataUriValidator.IsValid)
                .WithMessage("Please provide a valid PNG signature image within the allowed size and dimensions.");

            RuleFor(x => x.PdpaConsent)
                .Equal(true)
                .WithMessage("Please confirm the PDPA privacy notice before submitting.");
        }
    }
}
