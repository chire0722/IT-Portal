using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey
{
    public sealed class UpdateSoftwareKeyCommandValidator : AbstractValidator<UpdateSoftwareKeyCommand>
    {
        public UpdateSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.KeyValue).NotEmpty();
        }
    }
}
