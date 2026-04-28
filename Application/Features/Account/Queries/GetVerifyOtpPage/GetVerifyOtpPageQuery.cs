// Project comment: GetVerifyOtpPageQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Features.Account;
using IT_Form.Data;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Account.Queries.GetVerifyOtpPage;

/// <summary>
/// Carries data needed to execute the GetVerifyOtpPage query.
/// </summary>
public sealed record GetVerifyOtpPageQuery(
    string Email,
    string? Code = null) : IRequest<VerifyOtpViewModel>;

/// <summary>
/// Handles the GetVerifyOtpPageQuery query workflow.
/// </summary>
public sealed class GetVerifyOtpPageQueryHandler : IRequestHandler<GetVerifyOtpPageQuery, VerifyOtpViewModel>
{
    private readonly ApplicationDbContext _context;

    public GetVerifyOtpPageQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<VerifyOtpViewModel> Handle(GetVerifyOtpPageQuery request, CancellationToken cancellationToken)
    {
        var userEmail = await _context.Users
            .AsNoTracking()
            .Where(user => user.Email == request.Email)
            .Select(user => user.Email)
            .FirstOrDefaultAsync(cancellationToken);

        var remainingSeconds = 0;
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            var lastRequestedAtUtc = await _context.OtpRecords
                .AsNoTracking()
                .Where(otp => otp.Email == userEmail)
                .OrderByDescending(otp => otp.CreatedAt)
                .Select(otp => (DateTime?)otp.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRequestedAtUtc.HasValue)
            {
                var resendAvailableAtUtc = lastRequestedAtUtc.Value.Add(AccountOtpSupport.RequestCooldown);
                if (resendAvailableAtUtc > DateTime.UtcNow)
                {
                    remainingSeconds = (int)Math.Ceiling((resendAvailableAtUtc - DateTime.UtcNow).TotalSeconds);
                }
            }
        }

        return new VerifyOtpViewModel
        {
            Email = request.Email,
            Code = request.Code ?? string.Empty,
            RemainingCooldownSeconds = remainingSeconds
        };
    }
}
