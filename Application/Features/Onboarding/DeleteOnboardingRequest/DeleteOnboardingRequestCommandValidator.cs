// Project comment: DeleteOnboardingRequestCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest
{
    /// <summary>
    /// Validates input for the DeleteOnboardingRequestCommand request.
    /// </summary>
    public sealed class DeleteOnboardingRequestCommandValidator : AbstractValidator<DeleteOnboardingRequestCommand>
    {
        public DeleteOnboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

