using FluentValidation;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailGroups.Writes.CreateEmailGroup;

public sealed class CreateEmailGroupCommandValidator : AbstractValidator<CreateEmailGroupCommand>
{
    public CreateEmailGroupCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.GroupName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.GroupEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(100)
            .EmailAddress()
            .MustAsync(async (email, cancellationToken) =>
                !await context.EmailGroups
                    .AsNoTracking()
                    .AnyAsync(
                        group => group.GroupEmail.ToUpper() == email.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Email group email must be unique.");
    }
}
