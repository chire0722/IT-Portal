using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.RejectOnboardingRequest
{
    public sealed class RejectOnboardingRequestCommandValidator : AbstractValidator<RejectOnboardingRequestCommand>
    {
        public RejectOnboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("A rejection reason is required.");
        }
    }
}
