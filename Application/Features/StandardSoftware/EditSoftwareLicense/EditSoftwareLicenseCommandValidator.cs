// Project comment: EditSoftwareLicenseCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense
{
    /// <summary>
    /// Validates input for the EditSoftwareLicenseCommand request.
    /// </summary>
    public sealed class EditSoftwareLicenseCommandValidator : AbstractValidator<EditSoftwareLicenseCommand>
    {
        public EditSoftwareLicenseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SoftwareName)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
            RuleFor(x => x.VendorName).MaximumLength(100);
            RuleFor(x => x.SupportContact).MaximumLength(256);
            RuleFor(x => x.ContractLimit)
                .InclusiveBetween(0, 5000)
                .When(x => x.IsSubscription && !x.IsFree)
                .WithMessage("Paid subscription seat limits must be between 0 and 5,000.");
        }
    }
}
