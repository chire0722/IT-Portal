using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.RejectOffboardingRequest
{
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
