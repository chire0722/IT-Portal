// Project comment: AddSoftwareKeyCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    /// <summary>
    /// Validates input for the AddSoftwareKeyCommand request.
    /// </summary>
    public sealed class AddSoftwareKeyCommandValidator : AbstractValidator<AddSoftwareKeyCommand>
    {
        public AddSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.SoftwareId).GreaterThan(0);
            RuleFor(x => x.KeyValue).NotEmpty();
        }
    }
}
