// Project comment: DeleteSoftwareKeyCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey
{
    /// <summary>
    /// Validates input for the DeleteSoftwareKeyCommand request.
    /// </summary>
    public sealed class DeleteSoftwareKeyCommandValidator : AbstractValidator<DeleteSoftwareKeyCommand>
    {
        public DeleteSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
