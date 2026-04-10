using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    public sealed class DeleteSoftwareLicenseCommandValidator : AbstractValidator<DeleteSoftwareLicenseCommand>
    {
        public DeleteSoftwareLicenseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
