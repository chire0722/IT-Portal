using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    public sealed class ProcessOnboardingRejectionCommandValidator : AbstractValidator<ProcessOnboardingRejectionCommand>
    {
        public ProcessOnboardingRejectionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("A rejection reason is required.");
        }
    }
}
