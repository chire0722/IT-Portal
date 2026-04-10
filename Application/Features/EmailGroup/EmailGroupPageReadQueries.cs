using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailGroup
{
    public sealed record GetEmailGroupIndexPageQuery() : IRequest<ProcessResult<List<EmailGroupIndexListItem>>>;
    public sealed record GetEmailGroupCreatePageQuery(EmailGroupEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<EmailGroupEditorPageViewModel>>;
    public sealed record GetEmailGroupEditPageQuery(int Id, EmailGroupEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<EmailGroupEditorPageViewModel>>;
    public sealed record GetEmailGroupMembersPageQuery(int Id, int? OnboardingRequestId = null) : IRequest<ProcessResult<EmailGroupMembersPageViewModel>>;

    public sealed class GetEmailGroupIndexPageQueryHandler : IRequestHandler<GetEmailGroupIndexPageQuery, ProcessResult<List<EmailGroupIndexListItem>>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<List<EmailGroupIndexListItem>>> Handle(GetEmailGroupIndexPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var groupsQuery = _context.EmailGroups
                .AsNoTracking()
                .OrderBy(e => e.GroupName)
                .AsQueryable();

            if (accessibleCompanyIds is { Count: > 0 })
            {
                groupsQuery = groupsQuery.Where(group =>
                    group.EmailGroupCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            var groups = await groupsQuery
                .ProjectToType<EmailGroupIndexListItem>()
                .ToListAsync(cancellationToken);

            return ProcessResult<List<EmailGroupIndexListItem>>.Success(groups);
        }
    }

    public sealed class GetEmailGroupCreatePageQueryHandler : IRequestHandler<GetEmailGroupCreatePageQuery, ProcessResult<EmailGroupEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<EmailGroupEditorPageViewModel>> Handle(GetEmailGroupCreatePageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true)
                : null;

            if (request.SelectedCompanyIds != null &&
                accessibleCompanyIds is { Count: > 0 } &&
                !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<EmailGroupEditorPageViewModel>.Forbidden();
            }

            var model = request.Model?.Adapt<EmailGroupEditorPageViewModel>() ?? new EmailGroupEditorPageViewModel();
            model.SelectedCompanyIds = request.SelectedCompanyIds?.ToList() ?? [];
            model.CompanyOptions = await PageQueryLookupSupport.LoadActiveCompaniesAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<EmailGroupEditorPageViewModel>.Success(model);
        }
    }

    public sealed class GetEmailGroupEditPageQueryHandler : IRequestHandler<GetEmailGroupEditPageQuery, ProcessResult<EmailGroupEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupEditPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<EmailGroupEditorPageViewModel>> Handle(GetEmailGroupEditPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            EmailGroupEditorPageViewModel? model;
            if (request.Model != null)
            {
                model = request.Model.Adapt<EmailGroupEditorPageViewModel>();
            }
            else
            {
                model = await _context.EmailGroups
                    .AsNoTracking()
                    .Where(group => group.Id == request.Id)
                    .ProjectToType<EmailGroupEditorPageViewModel>()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (model == null)
            {
                return ProcessResult<EmailGroupEditorPageViewModel>.NotFound();
            }

            if (accessibleCompanyIds is { Count: > 0 } && !model.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<EmailGroupEditorPageViewModel>.Forbidden();
            }

            if (request.SelectedCompanyIds != null)
            {
                if (accessibleCompanyIds is { Count: > 0 } && !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<EmailGroupEditorPageViewModel>.Forbidden();
                }

                model.SelectedCompanyIds = request.SelectedCompanyIds.ToList();
            }

            model.CompanyOptions = await PageQueryLookupSupport.LoadActiveCompaniesAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<EmailGroupEditorPageViewModel>.Success(model);
        }
    }

    public sealed class GetEmailGroupMembersPageQueryHandler : IRequestHandler<GetEmailGroupMembersPageQuery, ProcessResult<EmailGroupMembersPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupMembersPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<EmailGroupMembersPageViewModel>> Handle(GetEmailGroupMembersPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var group = await _context.EmailGroups
                .AsNoTracking()
                .Where(emailGroup => emailGroup.Id == request.Id)
                .Select(emailGroup => new
                {
                    emailGroup.Id,
                    emailGroup.GroupName,
                    emailGroup.GroupEmail,
                    CompanyIds = emailGroup.EmailGroupCompanies.Select(link => link.CompanyId).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (group == null)
                return ProcessResult<EmailGroupMembersPageViewModel>.NotFound();

            if (accessibleCompanyIds is { Count: > 0 } && !group.CompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<EmailGroupMembersPageViewModel>.Forbidden();
            }

            var membersQuery = _context.RequestEmailGroups
                .AsNoTracking()
                .Where(r => r.EmailGroupId == request.Id);

            if (scopedCompanies.Count > 0)
            {
                var accessibleOnboardingRequestIds = CompanyScopeHelper
                    .ApplyOnboardingCompanyScope(_context.OnboardingRequests.AsNoTracking(), scopedCompanies)
                    .Select(onboarding => onboarding.Id);

                membersQuery = membersQuery.Where(r =>
                    r.OnboardingRequestId.HasValue &&
                    accessibleOnboardingRequestIds.Contains(r.OnboardingRequestId.Value));
            }

            var members = await membersQuery
                .OrderBy(r => r.OnboardingRequest!.FullName)
                .ProjectToType<EmailGroupMemberListItem>()
                .ToListAsync(cancellationToken);

            var currentEmployees = await PageQueryLookupSupport.LoadCurrentEmployeesAsync(_context, _currentUserAccessor.User, cancellationToken);
            var currentEmployeeDisplayName = request.OnboardingRequestId.HasValue &&
                                             currentEmployees.Any(employee => employee.Id == request.OnboardingRequestId.Value)
                ? await PageQueryLookupSupport.LoadCurrentEmployeeDisplayNameAsync(_context, request.OnboardingRequestId, cancellationToken)
                : null;

            return ProcessResult<EmailGroupMembersPageViewModel>.Success(new EmailGroupMembersPageViewModel
            {
                GroupId = group.Id,
                GroupName = group.GroupName,
                GroupEmail = group.GroupEmail,
                CurrentEmployeeId = request.OnboardingRequestId,
                CurrentEmployeeDisplayName = currentEmployeeDisplayName,
                CurrentEmployees = currentEmployees,
                Members = members
            });
        }
    }
}
