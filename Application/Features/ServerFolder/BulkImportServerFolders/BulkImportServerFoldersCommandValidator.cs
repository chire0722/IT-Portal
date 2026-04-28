// Project comment: BulkImportServerFoldersCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.ServerFolder.BulkImportServerFolders
{
    /// <summary>
    /// Validates input for the BulkImportServerFoldersCommand request.
    /// </summary>
    public sealed class BulkImportServerFoldersCommandValidator : AbstractValidator<BulkImportServerFoldersCommand>
    {
        public BulkImportServerFoldersCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .Must(f => f != null && f.Length > 0)
                .WithMessage("Please choose a valid Excel file.");
        }
    }
}
