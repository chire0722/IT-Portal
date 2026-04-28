// Project comment: UserManagementPageQueries.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.UserManagement
{
    /// <summary>
    /// Carries data needed to execute the GetUserManagementIndexPage query.
    /// </summary>
    public sealed record GetUserManagementIndexPageQuery() : IRequest<ProcessResult<List<UserManagementIndexListItem>>>;
    /// <summary>
    /// Carries data needed to execute the GetUserManagementCreatePage query.
    /// </summary>
    public sealed record GetUserManagementCreatePageQuery(UserManagementViewModel? Model = null) : IRequest<ProcessResult<UserManagementEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetUserManagementEditPage query.
    /// </summary>
    public sealed record GetUserManagementEditPageQuery(string Id, UserManagementViewModel? Model = null) : IRequest<ProcessResult<UserManagementEditorPageViewModel>>;

    /// <summary>
    /// Handles the GetUserManagementIndexPageQuery query workflow.
    /// </summary>
    public sealed class GetUserManagementIndexPageQueryHandler : IRequestHandler<GetUserManagementIndexPageQuery, ProcessResult<List<UserManagementIndexListItem>>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetUserManagementIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<List<UserManagementIndexListItem>>> Handle(GetUserManagementIndexPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var usersQuery = _context.Users
                .AsNoTracking()
                .Include(u => u.UserTitles)
                .Include(u => u.UserCompanies)
                .OrderBy(u => u.FullName)
                .AsQueryable();

            if (scopedCompanies.Count > 0)
            {
                usersQuery = usersQuery.Where(user => user.UserCompanies.Any(company =>
                    scopedCompanies.Any(scopedCompany =>
                        scopedCompany == company.CompanyName ||
                        scopedCompany.StartsWith(company.CompanyName + " (") ||
                        company.CompanyName.StartsWith(scopedCompany + " ("))));
            }

            var users = await usersQuery
                .Select(u => new UserManagementIndexListItem
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Username = u.UserName ?? string.Empty,
                    Email = u.Email,
                    Department = u.Department,
                    IsActive = u.IsActive,
                    Titles = u.UserTitles
                        .Select(title => title.Title)
                        .OrderBy(title => title)
                        .ToList(),
                    Companies = u.UserCompanies
                        .Select(company => company.CompanyName)
                        .OrderBy(company => company)
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return ProcessResult<List<UserManagementIndexListItem>>.Success(users);
        }
    }

    /// <summary>
    /// Handles the GetUserManagementCreatePageQuery query workflow.
    /// </summary>
    public sealed class GetUserManagementCreatePageQueryHandler : IRequestHandler<GetUserManagementCreatePageQuery, ProcessResult<UserManagementEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetUserManagementCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<UserManagementEditorPageViewModel>> Handle(GetUserManagementCreatePageQuery request, CancellationToken cancellationToken)
        {
            var companies = await UserManagementPageQueryHelpers.LoadCompaniesAsync(_context, _currentUserAccessor.User, cancellationToken);
            var departments = await UserManagementPageQueryHelpers.LoadDepartmentsAsync(_context, _currentUserAccessor.User, cancellationToken);
            var model = request.Model ?? new UserManagementViewModel();

            return ProcessResult<UserManagementEditorPageViewModel>.Success(
                UserManagementPageQueryHelpers.BuildEditorPage(model, departments, companies));
        }
    }

    /// <summary>
    /// Handles the GetUserManagementEditPageQuery query workflow.
    /// </summary>
    public sealed class GetUserManagementEditPageQueryHandler : IRequestHandler<GetUserManagementEditPageQuery, ProcessResult<UserManagementEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetUserManagementEditPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<UserManagementEditorPageViewModel>> Handle(GetUserManagementEditPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            UserManagementViewModel? model = request.Model;
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserTitles)
                .Include(u => u.UserCompanies)
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (user == null)
            {
                return ProcessResult<UserManagementEditorPageViewModel>.NotFound();
            }

            if (!CompanyScopeHelper.AreAllCompaniesAllowed(scopedCompanies, user.UserCompanies.Select(company => company.CompanyName)))
            {
                return ProcessResult<UserManagementEditorPageViewModel>.Forbidden();
            }

            if (model == null)
            {
                model = user.Adapt<UserManagementViewModel>();
            }

            var companies = await UserManagementPageQueryHelpers.LoadCompaniesAsync(_context, _currentUserAccessor.User, cancellationToken);
            var departments = await UserManagementPageQueryHelpers.LoadDepartmentsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<UserManagementEditorPageViewModel>.Success(
                UserManagementPageQueryHelpers.BuildEditorPage(model, departments, companies));
        }
    }

    /// <summary>
    /// Defines the UserManagementPageQueryHelpers class used by the IT Form application.
    /// </summary>
    internal static class UserManagementPageQueryHelpers
    {
        /// <summary>
        /// Runs the LoadDepartmentsAsync operation.
        /// </summary>
        internal static async Task<List<UserManagementDepartmentOption>> LoadDepartmentsAsync(
            ApplicationDbContext context,
            System.Security.Claims.ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
            var accessibleCompanyIds = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(
                context,
                user,
                cancellationToken,
                activeOnly: true);

            var departmentsQuery = context.Departments
                .AsNoTracking()
                .Where(department => department.IsActive)
                .OrderBy(department => department.Name)
                .AsQueryable();

            if (accessibleCompanyIds != null)
            {
                departmentsQuery = departmentsQuery.Where(department =>
                    !department.DepartmentCompanies.Any() ||
                    department.DepartmentCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            return await departmentsQuery
                .Select(department => new UserManagementDepartmentOption
                {
                    Name = department.Name
                })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Runs the LoadCompaniesAsync operation.
        /// </summary>
        internal static async Task<List<UserManagementCompanyOption>> LoadCompaniesAsync(
            ApplicationDbContext context,
            System.Security.Claims.ClaimsPrincipal? user,
            CancellationToken cancellationToken)
            => (await ItAdminCompanyScopeSupport.LoadAccessibleCompanyDisplayNamesAsync(context, user, cancellationToken))
                .Select(company => new UserManagementCompanyOption
                {
                    DisplayName = company
                })
                .ToList();

        /// <summary>
        /// Builds the value returned by the BuildEditorPage operation.
        /// </summary>
        internal static UserManagementEditorPageViewModel BuildEditorPage(
            UserManagementViewModel model,
            List<UserManagementDepartmentOption> departments,
            List<UserManagementCompanyOption> companies)
        {
            return new UserManagementEditorPageViewModel
            {
                Id = model.Id,
                FullName = model.FullName,
                Username = model.Username,
                Department = model.Department,
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                IsIT = model.IsIT,
                IsHR = model.IsHR,
                IsHOD = model.IsHOD,
                IsActive = model.IsActive,
                SelectedCompanies = [.. model.SelectedCompanies],
                Departments = departments,
                Companies = companies
            };
        }
    }
}
