// Project comment: DepartmentPageQueries.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Department
{
    /// <summary>
    /// Carries data needed to execute the GetDepartmentIndexPage query.
    /// </summary>
    public sealed record GetDepartmentIndexPageQuery() : IRequest<ProcessResult<List<DepartmentIndexListItem>>>;
    /// <summary>
    /// Carries data needed to execute the GetDepartmentCreatePage query.
    /// </summary>
    public sealed record GetDepartmentCreatePageQuery(DepartmentEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<DepartmentEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetDepartmentEditPage query.
    /// </summary>
    public sealed record GetDepartmentEditPageQuery(int Id, DepartmentEditorPageViewModel? Model = null, int[]? SelectedCompanyIds = null) : IRequest<ProcessResult<DepartmentEditorPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the DownloadDepartmentTemplate query.
    /// </summary>
    public sealed record DownloadDepartmentTemplateQuery() : IRequest<ProcessResult<BinaryFilePayload>>;

    /// <summary>
    /// Handles the GetDepartmentIndexPageQuery query workflow.
    /// </summary>
    public sealed class GetDepartmentIndexPageQueryHandler : IRequestHandler<GetDepartmentIndexPageQuery, ProcessResult<List<DepartmentIndexListItem>>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetDepartmentIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<List<DepartmentIndexListItem>>> Handle(GetDepartmentIndexPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            var departmentsQuery = _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .AsQueryable();

            if (accessibleCompanyIds is { Count: > 0 })
            {
                departmentsQuery = departmentsQuery.Where(department =>
                    department.DepartmentCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            var departments = await departmentsQuery
                .ProjectToType<DepartmentIndexListItem>()
                .ToListAsync(cancellationToken);

            return ProcessResult<List<DepartmentIndexListItem>>.Success(departments);
        }
    }

    /// <summary>
    /// Handles the GetDepartmentCreatePageQuery query workflow.
    /// </summary>
    public sealed class GetDepartmentCreatePageQueryHandler : IRequestHandler<GetDepartmentCreatePageQuery, ProcessResult<DepartmentEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetDepartmentCreatePageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<DepartmentEditorPageViewModel>> Handle(GetDepartmentCreatePageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true)
                : null;

            if (request.SelectedCompanyIds != null &&
                accessibleCompanyIds is { Count: > 0 } &&
                !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<DepartmentEditorPageViewModel>.Forbidden();
            }

            var model = request.Model?.Adapt<DepartmentEditorPageViewModel>() ?? new DepartmentEditorPageViewModel();
            model.SelectedCompanyIds = request.SelectedCompanyIds?.ToList() ?? [];
            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<DepartmentEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the GetDepartmentEditPageQuery query workflow.
    /// </summary>
    public sealed class GetDepartmentEditPageQueryHandler : IRequestHandler<GetDepartmentEditPageQuery, ProcessResult<DepartmentEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetDepartmentEditPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<DepartmentEditorPageViewModel>> Handle(GetDepartmentEditPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            DepartmentEditorPageViewModel? model;
            if (request.Model != null)
            {
                model = request.Model.Adapt<DepartmentEditorPageViewModel>();
            }
            else
            {
                model = await _context.Departments
                    .AsNoTracking()
                    .Where(department => department.Id == request.Id)
                    .ProjectToType<DepartmentEditorPageViewModel>()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (model == null)
            {
                return ProcessResult<DepartmentEditorPageViewModel>.NotFound();
            }

            if (accessibleCompanyIds is { Count: > 0 } && !model.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
            {
                return ProcessResult<DepartmentEditorPageViewModel>.Forbidden();
            }

            if (request.SelectedCompanyIds != null)
            {
                if (accessibleCompanyIds is { Count: > 0 } && !request.SelectedCompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<DepartmentEditorPageViewModel>.Forbidden();
                }

                model.SelectedCompanyIds = request.SelectedCompanyIds.ToList();
            }

            model.CompanyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(_context, _currentUserAccessor.User, cancellationToken);
            return ProcessResult<DepartmentEditorPageViewModel>.Success(model);
        }
    }

    /// <summary>
    /// Handles the DownloadDepartmentTemplateQuery query workflow.
    /// </summary>
    public sealed class DownloadDepartmentTemplateQueryHandler : IRequestHandler<DownloadDepartmentTemplateQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DownloadDepartmentTemplateQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(DownloadDepartmentTemplateQuery request, CancellationToken cancellationToken)
        {
            var companyDisplayNames = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyDisplayNamesAsync(
                _context,
                _currentUserAccessor.User,
                cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Departments");
            worksheet.Cell(1, 1).Value = "Name";
            worksheet.Cell(1, 2).Value = "Description";
            worksheet.Cell(1, 3).Value = "Company (Dropdown, comma-separated for multiple)";
            worksheet.Cell(1, 4).Value = "IsActive (TRUE/FALSE)";

            var headerRange = worksheet.Range("A1:D1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a9c56");
            headerRange.Style.Font.FontColor = XLColor.White;

            worksheet.Cell(2, 1).Value = "IT";
            worksheet.Cell(2, 2).Value = "Handles hardware, software, and support operations.";
            worksheet.Cell(2, 3).Value = companyDisplayNames.FirstOrDefault() ?? string.Empty;
            worksheet.Cell(2, 4).Value = "TRUE";

            var refSheet = ExcelTemplateDropdownHelper.CreateReferenceSheet(workbook);
            if (ExcelTemplateDropdownHelper.AddNamedList(
                workbook,
                refSheet,
                ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                1,
                "Active Company Names",
                companyDisplayNames))
            {
                ExcelTemplateDropdownHelper.ApplyDropdownFromNamedList(
                    worksheet,
                    "C2:C1000",
                    ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                    "Select Company",
                    "Select a company from the dropdown. For multiple companies, separate valid names with commas.",
                    "Invalid Company",
                    "Use the dropdown values or enter comma-separated company names exactly as listed on the Reference sheet.");
            }

            ExcelTemplateDropdownHelper.ApplyTrueFalseDropdown(worksheet, "D2:D1000");
            refSheet.Cell(1, 2).Value = "Notes";
            refSheet.Cell(2, 2).Value = "For multi-company rows, separate valid company names with commas in the import sheet cell.";
            refSheet.Column(2).Width = 78;
            worksheet.SheetView.FreezeRows(1);
            ExcelTemplateDropdownHelper.SetColumnWidths(worksheet, 24, 56, 42, 18);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Department_Template.xlsx"));
        }
    }
}
