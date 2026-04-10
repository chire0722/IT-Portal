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
    public sealed record GetSharedMailboxIndexPageQuery() : IRequest<ProcessResult<List<SharedMailboxIndexListItem>>>;
    public sealed record GetSharedMailboxCreatePageQuery(SharedMailboxEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<SharedMailboxEditorPageViewModel>>;
    public sealed record GetSharedMailboxEditPageQuery(int Id, SharedMailboxEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<SharedMailboxEditorPageViewModel>>;
    public sealed record GetSharedMailboxMembersPageQuery(int Id, int? OnboardingRequestId = null) : IRequest<ProcessResult<SharedMailboxMembersPageViewModel>>;

    public sealed class GetSharedMailboxIndexPageQueryHandler : IRequestHandler<GetSharedMailboxIndexPageQuery, ProcessResult<List<SharedMailboxIndexListItem>>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<List<SharedMailboxIndexListItem>>> Handle(GetSharedMailboxIndexPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var mailboxesQuery = _context.SharedMailboxes
                .AsNoTracking()
                .OrderBy(m => m.MailboxName)
                .AsQueryable();

            if (accessibleCompanyIds is { Count: > 0 })
            {
                mailboxesQuery = mailboxesQuery.Where(mailbox =>
                    mailbox.SharedMailboxCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            var mailboxes = await mailboxesQuery
                .ProjectToType<SharedMailboxIndexListItem>()
                .ToListAsync(cancellationToken);

            return ProcessResult<List<SharedMailboxIndexListItem>>.Success(mailboxes);
        }
    }

    public sealed class GetSharedMailboxCreatePageQueryHandler : IRequestHandler<GetSharedMailboxCreatePageQuery, ProcessResult<SharedMailboxEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<SharedMailboxEditorPageViewModel>> Handle(GetSharedMailboxCreatePageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true)
                : null;

            if (request.SelectedCompanyIds != null &&
                accessibleCompanyIds is { Count: > 0 } &&
                !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<SharedMailboxEditorPageViewModel>.Forbidden();
            }

            var model = request.Model?.Adapt<SharedMailboxEditorPageViewModel>() ?? new SharedMailboxEditorPageViewModel();
            model.SelectedCompanyIds = request.SelectedCompanyIds?.ToList() ?? [];
            model.CompanyOptions = await PageQueryLookupSupport.LoadActiveCompaniesAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<SharedMailboxEditorPageViewModel>.Success(model);
        }
    }

    public sealed class GetSharedMailboxEditPageQueryHandler : IRequestHandler<GetSharedMailboxEditPageQuery, ProcessResult<SharedMailboxEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxEditPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<SharedMailboxEditorPageViewModel>> Handle(GetSharedMailboxEditPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
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

            model.CompanyOptions = await PageQueryLookupSupport.LoadActiveCompaniesAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<SharedMailboxEditorPageViewModel>.Success(model);
        }
    }

    public sealed class GetSharedMailboxMembersPageQueryHandler : IRequestHandler<GetSharedMailboxMembersPageQuery, ProcessResult<SharedMailboxMembersPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxMembersPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<SharedMailboxMembersPageViewModel>> Handle(GetSharedMailboxMembersPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
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

            var currentEmployees = await PageQueryLookupSupport.LoadCurrentEmployeesAsync(_context, _currentUserAccessor.User, cancellationToken);
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
