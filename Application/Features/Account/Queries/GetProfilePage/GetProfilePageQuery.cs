// Project comment: GetProfilePageQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Account.Queries.GetProfilePage;

/// <summary>
/// Carries data needed to execute the GetProfilePage query.
/// </summary>
public sealed record GetProfilePageQuery(string UserId) : IRequest<ProcessResult<AccountProfilePageViewModel>>;

/// <summary>
/// Handles the GetProfilePageQuery query workflow.
/// </summary>
public sealed class GetProfilePageQueryHandler : IRequestHandler<GetProfilePageQuery, ProcessResult<AccountProfilePageViewModel>>
{
    private readonly ApplicationDbContext _context;

    public GetProfilePageQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ProcessResult<AccountProfilePageViewModel>> Handle(
        GetProfilePageQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.UserTitles)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return ProcessResult<AccountProfilePageViewModel>.NotFound();
        }

        return ProcessResult<AccountProfilePageViewModel>.Success(user.Adapt<AccountProfilePageViewModel>());
    }
}
