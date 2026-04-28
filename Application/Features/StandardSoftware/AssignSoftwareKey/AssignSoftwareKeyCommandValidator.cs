// Project comment: AssignSoftwareKeyCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey
{
    /// <summary>
    /// Validates input for the AssignSoftwareKeyCommand request.
    /// </summary>
    public sealed class AssignSoftwareKeyCommandValidator : AbstractValidator<AssignSoftwareKeyCommand>
    {
        public AssignSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
