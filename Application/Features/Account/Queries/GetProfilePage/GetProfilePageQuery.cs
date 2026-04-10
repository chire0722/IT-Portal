using IT_Form.Application.Common.Models;
using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Account.Queries.GetProfilePage;

public sealed record GetProfilePageQuery(string UserId) : IRequest<ProcessResult<AccountProfilePageViewModel>>;

public sealed class GetProfilePageQueryHandler : IRequestHandler<GetProfilePageQuery, ProcessResult<AccountProfilePageViewModel>>
{
    private readonly ApplicationDbContext _context;

    public GetProfilePageQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

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
