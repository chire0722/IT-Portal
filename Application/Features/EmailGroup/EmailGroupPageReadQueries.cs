// Project comment: EmailGroupPageReadQueries.cs is part of the IT Form application source code.

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
    /// <summary>
    /// Carries data needed to execute the GetEmailGroupIndexPage query.
    /// </summary>
    public sealed record GetEmailGroupIndexPageQuery(int? CompanyId = null) : IRequest<ProcessResult<EmailGroupIndexPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetEmailGroupCreatePage query.
    /// </summary>
    public sealed record GetEmailGroupCreatePageQuery(EmailGroupEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<EmailGroupEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetEmailGroupEditPage query.
    /// </summary>
    public sealed record GetEmailGroupEditPageQuery(int Id, EmailGroupEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<EmailGroupEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetEmailGroupMembersPage query.
    /// </summary>
    public sealed record GetEmailGroupMembersPageQuery(int Id, int? OnboardingRequestId = null) : IRequest<ProcessResult<EmailGroupMembersPageViewModel>>;

    /// <summary>
    /// Handles the GetEmailGroupIndexPageQuery query workflow.
    /// </summary>
    public sealed class GetEmailGroupIndexPageQueryHandler : IRequestHandler<GetEmailGroupIndexPageQuery, ProcessResult<EmailGroupIndexPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<EmailGroupIndexPageViewModel>> Handle(GetEmailGroupIndexPageQuery request, CancellationToken cancellationToken)
        {
            var companyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            if (request.CompanyId.HasValue && !companyOptions.Any(company => company.Id == request.CompanyId.Value))
            {
                return ProcessResult<EmailGroupIndexPageViewModel>.Forbidden();
            }

            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var groupsQuery = _context.EmailGroups
                .AsNoTracking()
                .OrderBy(e => e.GroupName)
                .AsQueryable();

            if (accessibleCompanyIds is { Count: > 0 })
            {
                groupsQuery = groupsQuery.Where(group =>
                    !group.EmailGroupCompanies.Any() ||
                    group.EmailGroupCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            if (request.CompanyId.HasValue)
            {
                groupsQuery = groupsQuery.Where(group =>
                    !group.EmailGroupCompanies.Any() ||
                    group.EmailGroupCompanies.Any(link => link.CompanyId == request.CompanyId.Value));
            }

            var groups = await groupsQuery
                .ProjectToType<EmailGroupIndexListItem>()
                .ToListAsync(cancellationToken);

            return ProcessResult<EmailGroupIndexPageViewModel>.Success(new EmailGroupIndexPageViewModel
            {
                SelectedCompanyId = request.CompanyId,
                CompanyOptions = companyOptions,
                Items = groups
            });
        }
    }

    /// <summary>
    /// Handles the GetEmailGroupCreatePageQuery query workflow.
    /// </summary>
    public sealed class GetEmailGroupCreatePageQueryHandler : IRequestHandler<GetEmailGroupCreatePageQuery, ProcessResult<EmailGroupEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<EmailGroupEditorPageViewModel>> Handle(GetEmailGroupCreatePageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true)
                : null;

            if (request.SelectedCompanyIds != null &&
                accessibleCompanyIds is { Count: > 0 } &&
                !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<EmailGroupEditorPageViewModel>.Forbidden();
            }

            var model = request.Model?.Adapt<EmailGroupEditorPageViewModel>() ?? new EmailGroupEditorPageViewModel();
            model.SelectedCompanyIds = request.SelectedCompanyIds?.ToList() ?? [];
            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<EmailGroupEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the GetEmailGroupEditPageQuery query workflow.
    /// </summary>
    public sealed class GetEmailGroupEditPageQueryHandler : IRequestHandler<GetEmailGroupEditPageQuery, ProcessResult<EmailGroupEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupEditPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<EmailGroupEditorPageViewModel>> Handle(GetEmailGroupEditPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
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

            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<EmailGroupEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the GetEmailGroupMembersPageQuery query workflow.
    /// </summary>
    public sealed class GetEmailGroupMembersPageQueryHandler : IRequestHandler<GetEmailGroupMembersPageQuery, ProcessResult<EmailGroupMembersPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupMembersPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<EmailGroupMembersPageViewModel>> Handle(GetEmailGroupMembersPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
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

            var currentEmployees = await ItAdminCompanyScopeSupport.LoadCurrentEmployeesAsync(_context, _currentUserAccessor.User, cancellationToken);
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
