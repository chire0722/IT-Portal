using FluentValidation;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.CreateSharedMailbox;

public sealed class CreateSharedMailboxCommandValidator : AbstractValidator<CreateSharedMailboxCommand>
{
    public CreateSharedMailboxCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.MailboxName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.MailboxAddress)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(100)
            .EmailAddress()
            .MustAsync(async (address, cancellationToken) =>
                !await context.SharedMailboxes
                    .AsNoTracking()
                    .AnyAsync(
                        mailbox => mailbox.MailboxAddress.ToUpper() == address.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Shared mailbox address must be unique.");
    }
}
