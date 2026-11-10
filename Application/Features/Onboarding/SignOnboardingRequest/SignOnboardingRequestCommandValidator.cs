// Project comment: SignOnboardingRequestCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Helpers;

namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    /// <summary>
    /// Validates input for the SignOnboardingRequestCommand request.
    /// </summary>
    public sealed class SignOnboardingRequestCommandValidator : AbstractValidator<SignOnboardingRequestCommand>
    {
        public SignOnboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureType)
                .NotEmpty();
            RuleFor(x => x.SignatureData)
                .Must(SignatureDataUriValidator.IsValid)
                .WithMessage("Signature must be a valid PNG image with bounded dimensions and size.");
        }
    }
}
