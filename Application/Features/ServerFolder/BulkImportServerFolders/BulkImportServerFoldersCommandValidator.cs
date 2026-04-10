using FluentValidation;

namespace IT_Form.Application.Features.ServerFolder.BulkImportServerFolders
{
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
