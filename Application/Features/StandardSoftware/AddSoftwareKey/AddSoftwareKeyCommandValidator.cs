using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    public sealed class AddSoftwareKeyCommandValidator : AbstractValidator<AddSoftwareKeyCommand>
    {
        public AddSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.SoftwareId).GreaterThan(0);
            RuleFor(x => x.KeyValue).NotEmpty();
        }
    }
}
