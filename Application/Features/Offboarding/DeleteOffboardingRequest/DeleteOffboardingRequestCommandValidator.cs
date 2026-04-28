// Project comment: DeleteOffboardingRequestCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest
{
    /// <summary>
    /// Validates input for the DeleteOffboardingRequestCommand request.
    /// </summary>
    public sealed class DeleteOffboardingRequestCommandValidator : AbstractValidator<DeleteOffboardingRequestCommand>
    {
        public DeleteOffboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
