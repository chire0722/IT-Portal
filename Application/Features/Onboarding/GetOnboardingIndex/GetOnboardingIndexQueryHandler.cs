using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    public sealed class GetOnboardingIndexQueryHandler : IRequestHandler<GetOnboardingIndexQuery, GetOnboardingIndexResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOnboardingIndexQueryHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<GetOnboardingIndexResult> Handle(GetOnboardingIndexQuery request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(user);
            var requests = _context.OnboardingRequests
                .AsNoTracking()
                .AsQueryable();

            if (scopedCompanies.Count > 0)
            {
                requests = CompanyScopeHelper.ApplyOnboardingCompanyScope(requests, scopedCompanies);
            }
            else if (!user.HasClaim("UserTitle", "IT") && !user.HasClaim("UserTitle", "HR"))
            {
                var currentUserName = user.Identity?.Name;
                requests = requests.Where(r => r.CreatedByUserId == currentUserName);
            }

            if (user.HasClaim("UserTitle", "IT"))
            {
                requests = requests.Where(r => r.Status != Models.Enums.RequestStatus.Draft);
            }

            requests = requests.ApplySearchFilter(request.SearchString, request.Status);

            var slaSetting = await _context.SlaSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.RequestType == "Onboarding", cancellationToken);

            const int pageSize = 10;
            var paginatedList = await PaginatedList<OnboardingIndexListItem>.CreateAsync(
                requests
                    .OrderByDescending(r => r.CreatedAt)
                    .ProjectToType<OnboardingIndexListItem>(),
                request.Page,
                pageSize);

            return new GetOnboardingIndexResult(
                paginatedList,
                request.SearchString,
                request.Status,
                slaSetting?.TargetDays ?? 3);
        }

    }
}
