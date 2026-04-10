using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey
{
    public sealed class AssignSoftwareKeyCommandValidator : AbstractValidator<AssignSoftwareKeyCommand>
    {
        public AssignSoftwareKeyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
