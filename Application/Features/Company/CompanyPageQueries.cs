// Project comment: CompanyPageQueries.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Company;

/// <summary>
/// Carries data needed to execute the GetCompanyIndexPage query.
/// </summary>
public sealed record GetCompanyIndexPageQuery() : IRequest<ProcessResult<List<CompanyIndexListItem>>>;

/// <summary>
/// Carries data needed to execute the GetCompanyCreatePage query.
/// </summary>
public sealed record GetCompanyCreatePageQuery(CompanyEditorPageViewModel? Model = null)
    : IRequest<ProcessResult<CompanyEditorPageViewModel>>;

/// <summary>
/// Carries data needed to execute the GetCompanyEditPage query.
/// </summary>
public sealed record GetCompanyEditPageQuery(int Id, CompanyEditorPageViewModel? Model = null)
    : IRequest<ProcessResult<CompanyEditorPageViewModel>>;

/// <summary>
/// Handles the GetCompanyIndexPageQuery query workflow.
/// </summary>
public sealed class GetCompanyIndexPageQueryHandler
    : IRequestHandler<GetCompanyIndexPageQuery, ProcessResult<List<CompanyIndexListItem>>>
{
    private readonly ApplicationDbContext _context;

    public GetCompanyIndexPageQueryHandler(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
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

/// <summary>
/// Handles the GetCompanyCreatePageQuery query workflow.
/// </summary>
public sealed class GetCompanyCreatePageQueryHandler
    : IRequestHandler<GetCompanyCreatePageQuery, ProcessResult<CompanyEditorPageViewModel>>
{
    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public Task<ProcessResult<CompanyEditorPageViewModel>> Handle(GetCompanyCreatePageQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ProcessResult<CompanyEditorPageViewModel>.Success(
            request.Model ?? new CompanyEditorPageViewModel()));
    }
}

/// <summary>
/// Handles the GetCompanyEditPageQuery query workflow.
/// </summary>
public sealed class GetCompanyEditPageQueryHandler
    : IRequestHandler<GetCompanyEditPageQuery, ProcessResult<CompanyEditorPageViewModel>>
{
    private readonly ApplicationDbContext _context;

    public GetCompanyEditPageQueryHandler(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
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
