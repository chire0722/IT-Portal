// Project comment: RejectOnboardingRequestCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.RejectOnboardingRequest
{
    /// <summary>
    /// Validates input for the RejectOnboardingRequestCommand request.
    /// </summary>
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
