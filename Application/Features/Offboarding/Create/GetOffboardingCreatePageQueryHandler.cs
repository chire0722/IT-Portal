// Project comment: GetOffboardingCreatePageQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Create
{
    /// <summary>
    /// Handles the GetOffboardingCreatePageQuery query workflow.
    /// </summary>
    public sealed class GetOffboardingCreatePageQueryHandler
        : IRequestHandler<GetOffboardingCreatePageQuery, ProcessResult<OffboardingCreatePageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOffboardingCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<OffboardingCreatePageViewModel>> Handle(GetOffboardingCreatePageQuery request, CancellationToken cancellationToken)
        {
            var viewModel = new OffboardingCreatePageViewModel
            {
                LastDayOfWork = DateTime.Today.AddDays(14)
            };

            var employeesWithActiveRequests = await _context.OffboardingRequests
                .Where(r => r.Status != RequestStatus.Rejected)
                .Select(r => r.OnboardingRequestId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (request.OnboardingRequestId.HasValue && employeesWithActiveRequests.Contains(request.OnboardingRequestId.Value))
            {
                return ProcessResult<OffboardingCreatePageViewModel>.Failure(
                    "An active offboarding request already exists for this employee.",
                    "Index");
            }

            var employeeQuery = _context.OnboardingRequests
                .Where(o => (o.Status == RequestStatus.Completed || o.Status == RequestStatus.Approved) && !employeesWithActiveRequests.Contains(o.Id));

            var user = _currentUserAccessor.User;
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(user);
            if (scopedCompanies.Count > 0)
            {
                employeeQuery = CompanyScopeHelper.ApplyOnboardingCompanyScope(employeeQuery, scopedCompanies);
            }

            viewModel.Employees = await employeeQuery
                .OrderBy(o => o.FullName)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = $"{o.FullName} ({o.Department})"
                })
                .ToListAsync(cancellationToken);

            if (request.OnboardingRequestId.HasValue)
            {
                var onboarding = await _context.OnboardingRequests
                    .FirstOrDefaultAsync(o => o.Id == request.OnboardingRequestId.Value, cancellationToken);

                if (onboarding != null)
                {
                    if (!CompanyScopeHelper.CanAccessCompany(user, onboarding.Branch))
                    {
                        return ProcessResult<OffboardingCreatePageViewModel>.Forbidden();
                    }

                    viewModel.OnboardingRequestId = onboarding.Id;
                    viewModel.FullName = onboarding.FullName ?? string.Empty;
                    viewModel.EmployeeID = onboarding.EmployeeID ?? string.Empty;
                    viewModel.Department = onboarding.Department ?? string.Empty;
                    viewModel.Branch = onboarding.Branch;
                    viewModel.HODEmail = onboarding.HODEmail ?? string.Empty;
                    viewModel.OtherDepartmentDetails = onboarding.OtherDepartmentDetails;
                    viewModel.OtherBranchDetails = onboarding.OtherBranchDetails;
                }
            }

            viewModel.AssignedHodList = await BuildHodListAsync(viewModel.Branch, viewModel.AssignedHodId, cancellationToken);
            return ProcessResult<OffboardingCreatePageViewModel>.Success(viewModel);
        }

        /// <summary>
        /// Builds the value returned by the BuildHodListAsync operation.
        /// </summary>
        private async Task<List<SelectListItem>> BuildHodListAsync(string? company, string? selectedId, CancellationToken cancellationToken)
        {
            var hodQuery = _context.Users
                .Where(u => u.IsActive && u.UserTitles.Any(t => t.Title == "HOD"));

            if (string.IsNullOrWhiteSpace(company))
            {
                return [];
            }

            return await hodQuery
                .Where(u => u.UserCompanies.Any(c =>
                    c.CompanyName == company ||
                    c.CompanyName == CompanyHelper.Normalize(company) ||
                    (!company.Contains(" (") && c.CompanyName.StartsWith(company + " ("))))
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.FullName,
                    Selected = u.Id == selectedId
                })
                .ToListAsync(cancellationToken);
        }
    }
}
