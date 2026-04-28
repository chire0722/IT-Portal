// Project comment: DeleteSoftwareLicenseCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    /// <summary>
    /// Validates input for the DeleteSoftwareLicenseCommand request.
    /// </summary>
    public sealed class DeleteSoftwareLicenseCommandValidator : AbstractValidator<DeleteSoftwareLicenseCommand>
    {
        public DeleteSoftwareLicenseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
