using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingStep1Options
{
    public sealed class GetOnboardingStep1OptionsQueryHandler
        : IRequestHandler<GetOnboardingStep1OptionsQuery, OnboardingStep1PageViewModel>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnboardingHodService _onboardingHodService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOnboardingStep1OptionsQueryHandler(
            ApplicationDbContext context,
            IOnboardingHodService onboardingHodService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _onboardingHodService = onboardingHodService;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<OnboardingStep1PageViewModel> Handle(GetOnboardingStep1OptionsQuery request, CancellationToken cancellationToken)
        {
            var model = request.Model;

            var allowedCompanies = await CompanyScopeHelper.LoadAccessibleCompanyDisplayNamesAsync(_context, _currentUserAccessor.User, cancellationToken);
            var accessibleCompanyIds = await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true);

            var departmentsQuery = _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .AsQueryable();
            if (accessibleCompanyIds.Count > 0)
            {
                departmentsQuery = departmentsQuery.Where(department =>
                    !department.DepartmentCompanies.Any() ||
                    department.DepartmentCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            var departments = await departmentsQuery
                .Select(d => new OnboardingDepartmentOptionViewModel
                {
                    Name = d.Name,
                    CompanyName = d.DepartmentCompanies
                        .OrderBy(dc => dc.Company != null ? dc.Company.Name : string.Empty)
                        .Select(dc => dc.Company != null ? dc.Company.Name : null)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var hodUsers = await _onboardingHodService.BuildHodSelectListAsync(
                model.Branch,
                model.HODEmail,
                cancellationToken);

            return new OnboardingStep1PageViewModel
            {
                Id = model.Id,
                CurrentStep = model.CurrentStep,
                FullName = model.FullName,
                EmployeeID = model.EmployeeID,
                Designation = model.Designation,
                Department = model.Department,
                Branch = model.Branch,
                DeskNumber = model.DeskNumber,
                JoinDate = model.JoinDate,
                EmailAddress = model.EmailAddress,
                ContactNumber = model.ContactNumber,
                HODEmail = model.HODEmail,
                IsPdpaConsented = model.IsPdpaConsented,
                OtherDepartmentDetails = model.OtherDepartmentDetails,
                OtherBranchDetails = model.OtherBranchDetails,
                AllowedCompanies = allowedCompanies,
                Departments = departments,
                HODUsers = hodUsers
            };
        }
    }
}
