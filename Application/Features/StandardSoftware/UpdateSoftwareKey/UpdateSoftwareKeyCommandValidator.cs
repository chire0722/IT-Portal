// Project comment: UpdateSoftwareKeyCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey
{
    /// <summary>
    /// Validates input for the UpdateSoftwareKeyCommand request.
    /// </summary>
    public sealed class UpdateSoftwareKeyCommandValidator : AbstractValidator<UpdateSoftwareKeyCommand>
    {
        public UpdateSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.KeyValue).NotEmpty();
        }
    }
}
