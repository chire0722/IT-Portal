using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    public sealed class SignOnboardingRequestCommandValidator : AbstractValidator<SignOnboardingRequestCommand>
    {
        public SignOnboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureType)
                .NotEmpty();
            RuleFor(x => x.SignatureData)
                .NotEmpty()
                .MaximumLength(500_000)
                .WithMessage("Signature data is invalid or too large.");
        }
    }
}
