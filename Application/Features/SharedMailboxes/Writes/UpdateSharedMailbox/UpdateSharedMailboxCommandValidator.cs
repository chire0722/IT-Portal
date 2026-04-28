// Project comment: UpdateSharedMailboxCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.UpdateSharedMailbox;

/// <summary>
/// Validates input for the UpdateSharedMailboxCommand request.
/// </summary>
public sealed class UpdateSharedMailboxCommandValidator : AbstractValidator<UpdateSharedMailboxCommand>
{
    public UpdateSharedMailboxCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.MailboxName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.MailboxAddress)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(100)
            .EmailAddress()
            .MustAsync(async (request, address, cancellationToken) =>
                !await context.SharedMailboxes
                    .AsNoTracking()
                    .AnyAsync(
                        mailbox => mailbox.Id != request.Id &&
                                   mailbox.MailboxAddress.ToUpper() == address.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Shared mailbox address must be unique.");
    }
}
