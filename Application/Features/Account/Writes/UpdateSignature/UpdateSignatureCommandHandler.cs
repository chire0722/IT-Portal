using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Validation;
using IT_Form.Data;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.Account.Writes.UpdateSignature;

public sealed class UpdateSignatureCommandHandler : IRequestHandler<UpdateSignatureCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UpdateSignatureCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<Unit> Handle(UpdateSignatureCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);
        }

        request.Adapt(user);

        (await _userManager.UpdateAsync(user)).EnsureSucceeded();

        var auditTime = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            CreatedAt = auditTime,
            Timestamp = auditTime,
            Action = "Update Profile",
            PerformedBy = user.FullName ?? user.UserName ?? "Unknown",
            EntityName = "User Account",
            EntityId = user.Id,
            Details = "User updated their digital signature.",
            IsSuccess = true
        });
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
