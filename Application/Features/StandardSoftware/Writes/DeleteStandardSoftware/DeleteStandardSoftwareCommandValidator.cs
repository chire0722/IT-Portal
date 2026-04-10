using FluentValidation;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.Writes.DeleteStandardSoftware;

public sealed class DeleteStandardSoftwareCommandValidator : AbstractValidator<DeleteStandardSoftwareCommand>
{
    public DeleteStandardSoftwareCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .MustAsync(async (id, cancellationToken) =>
                !await context.StandardSoftware
                    .Where(software => software.Id == id)
                    .SelectMany(software => software.Licenses)
                    .SelectMany(license => license.Keys)
                    .AnyAsync(key => key.IsAssigned, cancellationToken))
            .WithMessage("Cannot delete software. One or more versions have currently assigned seats/keys.");
    }
}
