// Project comment: UpdateManageAccessCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    /// <summary>
    /// Validates input for the UpdateManageAccessCommand request.
    /// </summary>
    public sealed class UpdateManageAccessCommandValidator : AbstractValidator<UpdateManageAccessCommand>
    {
        public UpdateManageAccessCommandValidator()
        {
            RuleFor(x => x.OnboardingId)
                .GreaterThan(0);
        }
    }
}
