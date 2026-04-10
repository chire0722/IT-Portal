using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest
{
    public sealed class DeleteOffboardingRequestCommandValidator : AbstractValidator<DeleteOffboardingRequestCommand>
    {
        public DeleteOffboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
