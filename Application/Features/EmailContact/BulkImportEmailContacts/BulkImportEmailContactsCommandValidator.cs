using FluentValidation;

namespace IT_Form.Application.Features.EmailContact.BulkImportEmailContacts
{
    public sealed class BulkImportEmailContactsCommandValidator : AbstractValidator<BulkImportEmailContactsCommand>
    {
        public BulkImportEmailContactsCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .Must(f => f != null && f.Length > 0)
                .WithMessage("Please select a valid Excel file.");
        }
    }
}
