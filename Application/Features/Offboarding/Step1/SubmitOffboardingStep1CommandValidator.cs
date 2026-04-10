using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    public sealed class SubmitOffboardingStep1CommandValidator : AbstractValidator<SubmitOffboardingStep1Command>
    {
        public SubmitOffboardingStep1CommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);

            RuleFor(x => x.Request)
                .NotNull();

            RuleFor(x => x)
                .Must(x => x.Id == x.Request.Id)
                .WithMessage("Request id mismatch.");
        }
    }
}
