using FluentValidation;

namespace IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes
{
    public sealed class ImportSharedMailboxesCommandValidator : AbstractValidator<ImportSharedMailboxesCommand>
    {
        public ImportSharedMailboxesCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .Must(f => f != null && f.Length > 0)
                .WithMessage("Please choose a valid Excel file.");
        }
    }
}
