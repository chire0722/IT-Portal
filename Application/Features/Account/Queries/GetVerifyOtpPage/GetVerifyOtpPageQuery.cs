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
    string? Code = null,
    string Purpose = OtpPurposes.PasswordReset) : IRequest<VerifyOtpViewModel>;

/// <summary>
/// Handles the GetVerifyOtpPageQuery query workflow.
/// </summary>
public sealed class GetVerifyOtpPageQueryHandler : IRequestHandler<GetVerifyOtpPageQuery, VerifyOtpViewModel>
{
    public GetVerifyOtpPageQueryHandler(ApplicationDbContext context)
    {
        // The page deliberately does not query account state or cooldowns. This keeps
        // the response identical for existing and non-existing email/username values.
        _ = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public Task<VerifyOtpViewModel> Handle(GetVerifyOtpPageQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new VerifyOtpViewModel
        {
            Email = request.Email,
            Code = request.Code ?? string.Empty,
            Purpose = request.Purpose,
            RemainingCooldownSeconds = 0
        });}
