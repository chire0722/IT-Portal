using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey
{
    public sealed class DeleteSoftwareKeyCommandValidator : AbstractValidator<DeleteSoftwareKeyCommand>
    {
        public DeleteSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
