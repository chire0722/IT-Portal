// Project comment: ImportSharedMailboxesCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes
{
    /// <summary>
    /// Validates input for the ImportSharedMailboxesCommand request.
    /// </summary>
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
