// Project comment: ServerFolderPageReadQueries.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ServerFolder
{
    /// <summary>
    /// Carries data needed to execute the GetServerFolderIndexPage query.
    /// </summary>
    public sealed record GetServerFolderIndexPageQuery(int? CompanyId = null) : IRequest<ProcessResult<ServerFolderIndexPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetServerFolderCreatePage query.
    /// </summary>
    public sealed record GetServerFolderCreatePageQuery(ServerFolderEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<ServerFolderEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetServerFolderEditPage query.
    /// </summary>
    public sealed record GetServerFolderEditPageQuery(int Id, ServerFolderEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<ServerFolderEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetServerFolderMembersPage query.
    /// </summary>
    public sealed record GetServerFolderMembersPageQuery(int Id, int? OnboardingRequestId = null) : IRequest<ProcessResult<ServerFolderMembersPageViewModel>>;

    /// <summary>
    /// Handles the GetServerFolderIndexPageQuery query workflow.
    /// </summary>
    public sealed class GetServerFolderIndexPageQueryHandler : IRequestHandler<GetServerFolderIndexPageQuery, ProcessResult<ServerFolderIndexPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetServerFolderIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<ServerFolderIndexPageViewModel>> Handle(GetServerFolderIndexPageQuery request, CancellationToken cancellationToken)
        {
            var companyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            if (request.CompanyId.HasValue && !companyOptions.Any(company => company.Id == request.CompanyId.Value))
            {
                return ProcessResult<ServerFolderIndexPageViewModel>.Forbidden();
            }

            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var serverFoldersQuery = _context.ServerFolders
                .AsNoTracking()
                .OrderBy(folder => folder.FolderName)
                .AsQueryable();

            if (accessibleCompanyIds is { Count: > 0 })
            {
                serverFoldersQuery = serverFoldersQuery.Where(folder =>
                    !folder.ServerFolderCompanies.Any() ||
                    folder.ServerFolderCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            if (request.CompanyId.HasValue)
            {
                serverFoldersQuery = serverFoldersQuery.Where(folder =>
                    !folder.ServerFolderCompanies.Any() ||
                    folder.ServerFolderCompanies.Any(link => link.CompanyId == request.CompanyId.Value));
            }

            var serverFolders = await serverFoldersQuery
                .ProjectToType<ServerFolderIndexListItem>()
                .ToListAsync(cancellationToken);

            return ProcessResult<ServerFolderIndexPageViewModel>.Success(new ServerFolderIndexPageViewModel
            {
                SelectedCompanyId = request.CompanyId,
                CompanyOptions = companyOptions,
                Items = serverFolders
            });
        }
    }

    /// <summary>
    /// Handles the GetServerFolderCreatePageQuery query workflow.
    /// </summary>
    public sealed class GetServerFolderCreatePageQueryHandler : IRequestHandler<GetServerFolderCreatePageQuery, ProcessResult<ServerFolderEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetServerFolderCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<ServerFolderEditorPageViewModel>> Handle(GetServerFolderCreatePageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true)
                : null;

            if (request.SelectedCompanyIds != null &&
                accessibleCompanyIds is { Count: > 0 } &&
                !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<ServerFolderEditorPageViewModel>.Forbidden();
            }

            var model = request.Model?.Adapt<ServerFolderEditorPageViewModel>() ?? new ServerFolderEditorPageViewModel();
            model.SelectedCompanyIds = request.SelectedCompanyIds?.ToList() ?? [];
            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<ServerFolderEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the GetServerFolderEditPageQuery query workflow.
    /// </summary>
    public sealed class GetServerFolderEditPageQueryHandler : IRequestHandler<GetServerFolderEditPageQuery, ProcessResult<ServerFolderEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetServerFolderEditPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<ServerFolderEditorPageViewModel>> Handle(GetServerFolderEditPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            ServerFolderEditorPageViewModel? model;
            if (request.Model != null)
            {
                model = request.Model.Adapt<ServerFolderEditorPageViewModel>();
            }
            else
            {
                model = await _context.ServerFolders
                    .AsNoTracking()
                    .Where(folder => folder.Id == request.Id)
                    .ProjectToType<ServerFolderEditorPageViewModel>()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (model == null)
            {
                return ProcessResult<ServerFolderEditorPageViewModel>.NotFound();
            }

            if (accessibleCompanyIds is { Count: > 0 } && !model.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<ServerFolderEditorPageViewModel>.Forbidden();
            }

            if (request.SelectedCompanyIds != null)
            {
                if (accessibleCompanyIds is { Count: > 0 } && !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<ServerFolderEditorPageViewModel>.Forbidden();
                }

                model.SelectedCompanyIds = request.SelectedCompanyIds.ToList();
            }

            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<ServerFolderEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the GetServerFolderMembersPageQuery query workflow.
    /// </summary>
    public sealed class GetServerFolderMembersPageQueryHandler : IRequestHandler<GetServerFolderMembersPageQuery, ProcessResult<ServerFolderMembersPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetServerFolderMembersPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<ServerFolderMembersPageViewModel>> Handle(GetServerFolderMembersPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var folder = await _context.ServerFolders
                .AsNoTracking()
                .Where(serverFolder => serverFolder.Id == request.Id)
                .Select(serverFolder => new
                {
                    serverFolder.Id,
                    serverFolder.FolderName,
                    CompanyIds = serverFolder.ServerFolderCompanies.Select(link => link.CompanyId).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (folder == null)
                return ProcessResult<ServerFolderMembersPageViewModel>.NotFound();

            if (accessibleCompanyIds is { Count: > 0 } && !folder.CompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<ServerFolderMembersPageViewModel>.Forbidden();
            }

            var membersQuery = _context.FolderPermissions
                .AsNoTracking()
                .Where(f => f.FolderName == folder.FolderName && (f.CanRead || f.CanModify))
                .AsQueryable();

            if (scopedCompanies.Count > 0)
            {
                var accessibleOnboardingRequestIds = CompanyScopeHelper
                    .ApplyOnboardingCompanyScope(_context.OnboardingRequests.AsNoTracking(), scopedCompanies)
                    .Select(onboarding => onboarding.Id);

                membersQuery = membersQuery.Where(permission =>
                    permission.OnboardingRequestId.HasValue &&
                    accessibleOnboardingRequestIds.Contains(permission.OnboardingRequestId.Value));
            }

            var members = await membersQuery
                .OrderBy(permission => permission.OnboardingRequest!.FullName)
                .ProjectToType<ServerFolderMemberListItem>()
                .ToListAsync(cancellationToken);

            var currentEmployees = await ItAdminCompanyScopeSupport.LoadCurrentEmployeesAsync(_context, _currentUserAccessor.User, cancellationToken);
            var currentEmployeeDisplayName = request.OnboardingRequestId.HasValue &&
                                             currentEmployees.Any(employee => employee.Id == request.OnboardingRequestId.Value)
                ? await PageQueryLookupSupport.LoadCurrentEmployeeDisplayNameAsync(_context, request.OnboardingRequestId, cancellationToken)
                : null;

            return ProcessResult<ServerFolderMembersPageViewModel>.Success(new ServerFolderMembersPageViewModel
            {
                FolderId = folder.Id,
                FolderName = folder.FolderName,
                CurrentEmployeeId = request.OnboardingRequestId,
                CurrentEmployeeDisplayName = currentEmployeeDisplayName,
                CurrentEmployees = currentEmployees,
                Members = members
            });
        }
    }
}
