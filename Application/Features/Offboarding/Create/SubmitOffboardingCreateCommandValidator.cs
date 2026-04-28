// Project comment: SubmitOffboardingCreateCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.Create
{
    /// <summary>
    /// Validates input for the SubmitOffboardingCreateCommand request.
    /// </summary>
    public sealed class SubmitOffboardingCreateCommandValidator : AbstractValidator<SubmitOffboardingCreateCommand>
    {
        public SubmitOffboardingCreateCommandValidator()
        {
            RuleFor(x => x.Request).NotNull();
            RuleFor(x => x.Request.OnboardingRequestId)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Please select a valid employee.");
            RuleFor(x => x.Request.LastDayOfWork)
                .GreaterThan(DateTime.MinValue)
                .WithMessage("Please select the employee's last working day.");
        }
    }
}
