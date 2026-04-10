using IT_Form.Application.Common.Models;
using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Company;

public sealed record GetCompanyIndexPageQuery() : IRequest<ProcessResult<List<CompanyIndexListItem>>>;

public sealed record GetCompanyCreatePageQuery(CompanyEditorPageViewModel? Model = null)
    : IRequest<ProcessResult<CompanyEditorPageViewModel>>;

public sealed record GetCompanyEditPageQuery(int Id, CompanyEditorPageViewModel? Model = null)
    : IRequest<ProcessResult<CompanyEditorPageViewModel>>;

public sealed class GetCompanyIndexPageQueryHandler
    : IRequestHandler<GetCompanyIndexPageQuery, ProcessResult<List<CompanyIndexListItem>>>
{
    private readonly ApplicationDbContext _context;

    public GetCompanyIndexPageQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<ProcessResult<List<CompanyIndexListItem>>> Handle(GetCompanyIndexPageQuery request, CancellationToken cancellationToken)
    {
        var companies = await _context.Companies
            .AsNoTracking()
            .OrderBy(company => company.Name)
            .ProjectToType<CompanyIndexListItem>()
            .ToListAsync(cancellationToken);

        return ProcessResult<List<CompanyIndexListItem>>.Success(companies);
    }
}

public sealed class GetCompanyCreatePageQueryHandler
    : IRequestHandler<GetCompanyCreatePageQuery, ProcessResult<CompanyEditorPageViewModel>>
{
    public Task<ProcessResult<CompanyEditorPageViewModel>> Handle(GetCompanyCreatePageQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ProcessResult<CompanyEditorPageViewModel>.Success(
            request.Model ?? new CompanyEditorPageViewModel()));
    }
}

public sealed class GetCompanyEditPageQueryHandler
    : IRequestHandler<GetCompanyEditPageQuery, ProcessResult<CompanyEditorPageViewModel>>
{
    private readonly ApplicationDbContext _context;

    public GetCompanyEditPageQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<ProcessResult<CompanyEditorPageViewModel>> Handle(GetCompanyEditPageQuery request, CancellationToken cancellationToken)
    {
        var model = request.Model ?? await _context.Companies
            .AsNoTracking()
            .Where(company => company.Id == request.Id)
            .ProjectToType<CompanyEditorPageViewModel>()
            .FirstOrDefaultAsync(cancellationToken);

        if (model == null)
        {
            return ProcessResult<CompanyEditorPageViewModel>.NotFound();
        }

        return ProcessResult<CompanyEditorPageViewModel>.Success(model);
    }
}
