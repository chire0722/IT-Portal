using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    public sealed class SubmitOffboardingStep2CommandValidator : AbstractValidator<SubmitOffboardingStep2Command>
    {
        public SubmitOffboardingStep2CommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Request).NotNull();
            RuleFor(x => x.Request.Id).Equal(x => x.Id);
            RuleFor(x => x.SaveAction).NotEmpty();
        }
    }
}
