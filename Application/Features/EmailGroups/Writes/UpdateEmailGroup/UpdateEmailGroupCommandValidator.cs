// Project comment: UpdateEmailGroupCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailGroups.Writes.UpdateEmailGroup;

/// <summary>
/// Validates input for the UpdateEmailGroupCommand request.
/// </summary>
public sealed class UpdateEmailGroupCommandValidator : AbstractValidator<UpdateEmailGroupCommand>
{
    public UpdateEmailGroupCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.GroupName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.GroupEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(100)
            .EmailAddress()
            .MustAsync(async (request, email, cancellationToken) =>
                !await context.EmailGroups
                    .AsNoTracking()
                    .AnyAsync(
                        group => group.Id != request.Id &&
                                 group.GroupEmail.ToUpper() == email.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Email group email must be unique.");
    }
}
