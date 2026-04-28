// Project comment: GetOffboardingIndexQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.GetOffboardingIndex
{
    /// <summary>
    /// Handles the GetOffboardingIndexQuery query workflow.
    /// </summary>
    public sealed class GetOffboardingIndexQueryHandler
        : IRequestHandler<GetOffboardingIndexQuery, ProcessResult<OffboardingIndexPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetOffboardingIndexQueryHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<OffboardingIndexPageViewModel>> Handle(GetOffboardingIndexQuery request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            var isIT = user.HasClaim("UserTitle", "IT");
            var isHR = user.HasClaim("UserTitle", "HR");
            var isHOD = user.HasClaim("UserTitle", "HOD");
            var scopedCompanies = HrRecordReadCompanyScopeSupport.GetScopedCompanies(user);

            IQueryable<OffboardingRequest> query = _context.OffboardingRequests
                .AsNoTracking()
                .AsQueryable();

            if (!isHR)
            {
                var hodEmailsLower = await UserHelper.GetHODEmailsAsync(_context, _currentUserAccessor.UserName ?? string.Empty);
                var userId = _userManager.GetUserId(user);

                query = query.Where(r =>
                    (isIT && (r.Status == RequestStatus.PendingITApproval || r.Status == RequestStatus.Completed || r.Status == RequestStatus.Rejected)) ||
                    (isHOD && (r.AssignedHodId == userId || (r.AssignedHodId == null && r.HODEmail != null && hodEmailsLower.Contains(r.HODEmail.ToLower())))));

                query = query.Where(r => r.Status != RequestStatus.Draft || (isHOD && r.AssignedHodId == userId));
            }

            if (scopedCompanies.Count > 0)
            {
                query = CompanyScopeHelper.ApplyCompanyScope(query, scopedCompanies, request => request.Branch);
            }

            if (!string.IsNullOrEmpty(request.SearchString))
            {
                var lowerSearch = request.SearchString.ToLower();
                query = query.Where(r =>
                    (r.FullName != null && r.FullName.ToLower().Contains(lowerSearch)) ||
                    (r.EmployeeID != null && r.EmployeeID.ToLower().Contains(lowerSearch)) ||
                    (r.Department != null && r.Department.ToLower().Contains(lowerSearch)));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(r => r.Status == request.Status.Value);
            }

            var slaTargetDays = (await _context.SlaSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.RequestType == "Offboarding", cancellationToken))?.TargetDays ?? 3;

            var paged = await PaginatedList<OffboardingIndexListItem>.CreateAsync(
                query
                    .OrderByDescending(r => r.CreatedAt)
                    .ProjectToType<OffboardingIndexListItem>(),
                request.Page,
                10);

            var companies = await _context.Companies
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var item in paged)
            {
                item.CompanyTimeZoneId = CompanyTimeZoneHelper.ResolveTimeZoneId(companies, item.Branch);
                var slaStart = item.HRSignedAt ?? item.RequestDate;
                item.SlaDeadline = CompanyBusinessHoursHelper.CalculateSlaDeadline(
                    companies,
                    item.Branch,
                    slaStart,
                    slaTargetDays);
            }

            return ProcessResult<OffboardingIndexPageViewModel>.Success(new OffboardingIndexPageViewModel
            {
                Requests = paged,
                CurrentFilter = request.SearchString,
                StatusFilter = request.Status,
                SlaTargetDays = slaTargetDays
            });
        }
    }
}
