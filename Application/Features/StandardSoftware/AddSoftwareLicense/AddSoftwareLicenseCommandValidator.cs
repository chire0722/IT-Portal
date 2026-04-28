// Project comment: AddSoftwareLicenseCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense
{
    /// <summary>
    /// Validates input for the AddSoftwareLicenseCommand request.
    /// </summary>
    public sealed class AddSoftwareLicenseCommandValidator : AbstractValidator<AddSoftwareLicenseCommand>
    {
        public AddSoftwareLicenseCommandValidator()
        {
            RuleFor(x => x.StandardSoftwareId).GreaterThan(0);
            RuleFor(x => x.SoftwareName).MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
            RuleFor(x => x.VendorName).MaximumLength(100);
            RuleFor(x => x.SupportContact).MaximumLength(256);
        }
    }
}
