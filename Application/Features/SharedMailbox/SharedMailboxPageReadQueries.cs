// Project comment: SharedMailboxPageReadQueries.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.SharedMailbox
{
    /// <summary>
    /// Carries data needed to execute the GetSharedMailboxIndexPage query.
    /// </summary>
    public sealed record GetSharedMailboxIndexPageQuery(int? CompanyId = null) : IRequest<ProcessResult<SharedMailboxIndexPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetSharedMailboxCreatePage query.
    /// </summary>
    public sealed record GetSharedMailboxCreatePageQuery(SharedMailboxEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<SharedMailboxEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetSharedMailboxEditPage query.
    /// </summary>
    public sealed record GetSharedMailboxEditPageQuery(int Id, SharedMailboxEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<SharedMailboxEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetSharedMailboxMembersPage query.
    /// </summary>
    public sealed record GetSharedMailboxMembersPageQuery(int Id, int? OnboardingRequestId = null) : IRequest<ProcessResult<SharedMailboxMembersPageViewModel>>;

    /// <summary>
    /// Handles the GetSharedMailboxIndexPageQuery query workflow.
    /// </summary>
    public sealed class GetSharedMailboxIndexPageQueryHandler : IRequestHandler<GetSharedMailboxIndexPageQuery, ProcessResult<SharedMailboxIndexPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<SharedMailboxIndexPageViewModel>> Handle(GetSharedMailboxIndexPageQuery request, CancellationToken cancellationToken)
        {
            var companyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            if (request.CompanyId.HasValue && !companyOptions.Any(company => company.Id == request.CompanyId.Value))
            {
                return ProcessResult<SharedMailboxIndexPageViewModel>.Forbidden();
            }

            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var mailboxesQuery = _context.SharedMailboxes
                .AsNoTracking()
                .OrderBy(m => m.MailboxName)
                .AsQueryable();

            if (accessibleCompanyIds is { Count: > 0 })
            {
                mailboxesQuery = mailboxesQuery.Where(mailbox =>
                    !mailbox.SharedMailboxCompanies.Any() ||
                    mailbox.SharedMailboxCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            if (request.CompanyId.HasValue)
            {
                mailboxesQuery = mailboxesQuery.Where(mailbox =>
                    !mailbox.SharedMailboxCompanies.Any() ||
                    mailbox.SharedMailboxCompanies.Any(link => link.CompanyId == request.CompanyId.Value));
            }

            var mailboxes = await mailboxesQuery
                .ProjectToType<SharedMailboxIndexListItem>()
                .ToListAsync(cancellationToken);

            return ProcessResult<SharedMailboxIndexPageViewModel>.Success(new SharedMailboxIndexPageViewModel
            {
                SelectedCompanyId = request.CompanyId,
                CompanyOptions = companyOptions,
                Items = mailboxes
            });
        }
    }

    /// <summary>
    /// Handles the GetSharedMailboxCreatePageQuery query workflow.
    /// </summary>
    public sealed class GetSharedMailboxCreatePageQueryHandler : IRequestHandler<GetSharedMailboxCreatePageQuery, ProcessResult<SharedMailboxEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<SharedMailboxEditorPageViewModel>> Handle(GetSharedMailboxCreatePageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true)
                : null;

            if (request.SelectedCompanyIds != null &&
                accessibleCompanyIds is { Count: > 0 } &&
                !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<SharedMailboxEditorPageViewModel>.Forbidden();
            }

            var model = request.Model?.Adapt<SharedMailboxEditorPageViewModel>() ?? new SharedMailboxEditorPageViewModel();
            model.SelectedCompanyIds = request.SelectedCompanyIds?.ToList() ?? [];
            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<SharedMailboxEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the GetSharedMailboxEditPageQuery query workflow.
    /// </summary>
    public sealed class GetSharedMailboxEditPageQueryHandler : IRequestHandler<GetSharedMailboxEditPageQuery, ProcessResult<SharedMailboxEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxEditPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<SharedMailboxEditorPageViewModel>> Handle(GetSharedMailboxEditPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            SharedMailboxEditorPageViewModel? model;
            if (request.Model != null)
            {
                model = request.Model.Adapt<SharedMailboxEditorPageViewModel>();
            }
            else
            {
                model = await _context.SharedMailboxes
                    .AsNoTracking()
                    .Where(mailbox => mailbox.Id == request.Id)
                    .ProjectToType<SharedMailboxEditorPageViewModel>()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (model == null)
            {
                return ProcessResult<SharedMailboxEditorPageViewModel>.NotFound();
            }

            if (accessibleCompanyIds is { Count: > 0 } && !model.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<SharedMailboxEditorPageViewModel>.Forbidden();
            }

            if (request.SelectedCompanyIds != null)
            {
                if (accessibleCompanyIds is { Count: > 0 } && !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<SharedMailboxEditorPageViewModel>.Forbidden();
                }

                model.SelectedCompanyIds = request.SelectedCompanyIds.ToList();
            }

            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<SharedMailboxEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the GetSharedMailboxMembersPageQuery query workflow.
    /// </summary>
    public sealed class GetSharedMailboxMembersPageQueryHandler : IRequestHandler<GetSharedMailboxMembersPageQuery, ProcessResult<SharedMailboxMembersPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxMembersPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<SharedMailboxMembersPageViewModel>> Handle(GetSharedMailboxMembersPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var mailbox = await _context.SharedMailboxes
                .AsNoTracking()
                .Where(sharedMailbox => sharedMailbox.Id == request.Id)
                .Select(sharedMailbox => new
                {
                    sharedMailbox.Id,
                    sharedMailbox.MailboxName,
                    sharedMailbox.MailboxAddress,
                    CompanyIds = sharedMailbox.SharedMailboxCompanies.Select(link => link.CompanyId).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (mailbox == null)
                return ProcessResult<SharedMailboxMembersPageViewModel>.NotFound();

            if (accessibleCompanyIds is { Count: > 0 } && !mailbox.CompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<SharedMailboxMembersPageViewModel>.Forbidden();
            }

            var membersQuery = _context.RequestSharedMailboxes
                .AsNoTracking()
                .Where(r => r.SharedMailboxId == request.Id);

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
                .ProjectToType<SharedMailboxMemberListItem>()
                .ToListAsync(cancellationToken);

            var currentEmployees = await ItAdminCompanyScopeSupport.LoadCurrentEmployeesAsync(_context, _currentUserAccessor.User, cancellationToken);
            var currentEmployeeDisplayName = request.OnboardingRequestId.HasValue &&
                                             currentEmployees.Any(employee => employee.Id == request.OnboardingRequestId.Value)
                ? await PageQueryLookupSupport.LoadCurrentEmployeeDisplayNameAsync(_context, request.OnboardingRequestId, cancellationToken)
                : null;

            return ProcessResult<SharedMailboxMembersPageViewModel>.Success(new SharedMailboxMembersPageViewModel
            {
                MailboxId = mailbox.Id,
                MailboxName = mailbox.MailboxName,
                MailboxAddress = mailbox.MailboxAddress,
                CurrentEmployeeId = request.OnboardingRequestId,
                CurrentEmployeeDisplayName = currentEmployeeDisplayName,
                CurrentEmployees = currentEmployees,
                Members = members
            });
        }
    }
}
