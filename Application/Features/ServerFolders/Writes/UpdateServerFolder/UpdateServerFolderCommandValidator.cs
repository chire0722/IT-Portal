using FluentValidation;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ServerFolders.Writes.UpdateServerFolder;

public sealed class UpdateServerFolderCommandValidator : AbstractValidator<UpdateServerFolderCommand>
{
    public UpdateServerFolderCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.FolderName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(async (request, folderName, cancellationToken) =>
                !await context.ServerFolders
                    .AsNoTracking()
                    .AnyAsync(
                        folder => folder.Id != request.Id &&
                                  folder.FolderName.ToUpper() == folderName.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Server folder name must be unique.");
    }
}
