// Project comment: RejectOffboardingRequestCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.RejectOffboardingRequest
{
    /// <summary>
    /// Validates input for the RejectOffboardingRequestCommand request.
    /// </summary>
    public sealed class RejectOffboardingRequestCommandValidator : AbstractValidator<RejectOffboardingRequestCommand>
    {
        public RejectOffboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("A rejection reason is required.");
        }
    }
}
