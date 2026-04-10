using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest
{
    public sealed class DeleteOnboardingRequestCommandValidator : AbstractValidator<DeleteOnboardingRequestCommand>
    {
        public DeleteOnboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

