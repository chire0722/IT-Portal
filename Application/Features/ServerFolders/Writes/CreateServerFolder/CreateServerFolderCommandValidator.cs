using FluentValidation;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder;

public sealed class CreateServerFolderCommandValidator : AbstractValidator<CreateServerFolderCommand>
{
    public CreateServerFolderCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.FolderName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(async (folderName, cancellationToken) =>
                !await context.ServerFolders
                    .AsNoTracking()
                    .AnyAsync(
                        folder => folder.FolderName.ToUpper() == folderName.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Server folder name must be unique.");
    }
}
