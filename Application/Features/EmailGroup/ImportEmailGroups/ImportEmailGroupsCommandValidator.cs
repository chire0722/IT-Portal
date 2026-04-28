// Project comment: ImportEmailGroupsCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.EmailGroup.ImportEmailGroups
{
    /// <summary>
    /// Validates input for the ImportEmailGroupsCommand request.
    /// </summary>
    public sealed class ImportEmailGroupsCommandValidator : AbstractValidator<ImportEmailGroupsCommand>
    {
        public ImportEmailGroupsCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .Must(f => f != null && f.Length > 0)
                .WithMessage("Please choose a valid Excel file.");
        }
    }
}
