using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware
{
    public sealed record GetStandardSoftwareIndexPageQuery(string? SearchString, bool? IsMandatory, bool? IsSubscription, int? CompanyId)
        : IRequest<ProcessResult<StandardSoftwareIndexPageViewModel>>;

    public sealed record GetStandardSoftwareCreatePageQuery(StandardSoftwareEditorPageViewModel? Model = null)
        : IRequest<ProcessResult<StandardSoftwareEditorPageViewModel>>;

    public sealed record GetStandardSoftwareEditPageQuery(int Id, StandardSoftwareEditorPageViewModel? Model = null)
        : IRequest<ProcessResult<StandardSoftwareEditorPageViewModel>>;

    public sealed class GetStandardSoftwareIndexPageQueryHandler
        : IRequestHandler<GetStandardSoftwareIndexPageQuery, ProcessResult<StandardSoftwareIndexPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetStandardSoftwareIndexPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<StandardSoftwareIndexPageViewModel>> Handle(GetStandardSoftwareIndexPageQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var companyOptions = await ItAdminCompanyScopeSupport.LoadCompanyOptionsAsync(
                _context,
                _currentUserAccessor.User,
                cancellationToken);
            var accessibleCompanyIds = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(
                _context,
                _currentUserAccessor.User,
                cancellationToken);

            if (request.CompanyId.HasValue && !companyOptions.Any(company => company.Id == request.CompanyId.Value))
            {
                return ProcessResult<StandardSoftwareIndexPageViewModel>.Forbidden();
            }

            var scopedLicenseQuery = _context.SoftwareLicenses
                .AsNoTracking()
                .AsQueryable();

            if (accessibleCompanyIds != null)
            {
                scopedLicenseQuery = scopedLicenseQuery.Where(license =>
                    !license.SoftwareLicenseCompanies.Any() ||
                    license.SoftwareLicenseCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            if (request.CompanyId.HasValue)
            {
                scopedLicenseQuery = scopedLicenseQuery.Where(license =>
                    !license.SoftwareLicenseCompanies.Any() ||
                    license.SoftwareLicenseCompanies.Any(link => link.CompanyId == request.CompanyId.Value));
            }

            var analytics = new StandardSoftwareIndexPageViewModel
            {
                TotalSoftwareCount = await _context.StandardSoftware
                    .AsNoTracking()
                    .CountAsync(cancellationToken),
                TotalVersionCount = await scopedLicenseQuery
                    .CountAsync(cancellationToken),
                TotalSeatCapacity = await scopedLicenseQuery
                    .Where(license => !license.IsFree)
                    .SumAsync(license => (int?)license.ContractLimit, cancellationToken) ?? 0,
                TotalAssignedSeats = await _context.SoftwareKeys
                    .AsNoTracking()
                    .CountAsync(key =>
                        key.IsAssigned &&
                        key.SoftwareLicense != null &&
                        !key.SoftwareLicense.IsFree &&
                        (!request.CompanyId.HasValue ||
                         !key.SoftwareLicense.SoftwareLicenseCompanies.Any() ||
                         key.SoftwareLicense.SoftwareLicenseCompanies.Any(link => link.CompanyId == request.CompanyId.Value)) &&
                        (accessibleCompanyIds == null ||
                         !key.SoftwareLicense.SoftwareLicenseCompanies.Any() ||
                         key.SoftwareLicense.SoftwareLicenseCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId))),
                        cancellationToken),
                ExpiredCount = await scopedLicenseQuery
                    .CountAsync(license => license.ExpiryDate.HasValue && license.ExpiryDate.Value <= today, cancellationToken),
                ExpiringSoonCount = await scopedLicenseQuery
                    .CountAsync(license =>
                        license.ExpiryDate.HasValue &&
                        license.ExpiryDate.Value > today &&
                        license.ExpiryDate.Value <= today.AddDays(30), cancellationToken)
            };

            var softwareList = await _context.StandardSoftware
                .AsNoTracking()
                .OrderBy(software => software.SoftwareName)
                .ProjectToType<StandardSoftwareIndexItem>()
                .ToListAsync(cancellationToken);

            if (accessibleCompanyIds != null || request.CompanyId.HasValue)
            {
                foreach (var software in softwareList)
                {
                    software.Licenses = software.Licenses
                        .Where(license =>
                            (accessibleCompanyIds == null || license.CompanyIds.Count == 0 || license.CompanyIds.Any(accessibleCompanyIds.Contains)) &&
                            (!request.CompanyId.HasValue || license.CompanyIds.Count == 0 || license.CompanyIds.Contains(request.CompanyId.Value)))
                        .ToList();
                }

                softwareList = softwareList
                    .Where(software => software.Licenses.Count > 0)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(request.SearchString))
            {
                var lowerSearch = request.SearchString.ToLowerInvariant();
                softwareList = softwareList
                    .Where(software =>
                        software.SoftwareName.ToLowerInvariant().Contains(lowerSearch) ||
                        software.Licenses.Any(license => license.SoftwareName.ToLowerInvariant().Contains(lowerSearch)))
                    .ToList();
            }

            if (request.IsMandatory.HasValue)
            {
                softwareList = softwareList
                    .Where(software => software.IsMandatory == request.IsMandatory.Value)
                    .ToList();
            }

            if (request.IsSubscription.HasValue)
            {
                foreach (var software in softwareList)
                {
                    software.Licenses = software.Licenses
                        .Where(license => license.IsSubscription == request.IsSubscription.Value)
                        .ToList();
                }

                softwareList = softwareList
                    .Where(software => software.Licenses.Count > 0)
                    .ToList();
            }

            analytics.CompanyOptions = companyOptions;
            analytics.SoftwareList = softwareList;
            analytics.TotalSoftwareCount = softwareList.Count;
            analytics.TotalVersionCount = softwareList.Sum(software => software.Licenses.Count);
            analytics.CurrentSearch = request.SearchString;
            analytics.IsMandatory = request.IsMandatory;
            analytics.IsSubscription = request.IsSubscription;
            analytics.SelectedCompanyId = request.CompanyId;

            await _context.LogActionAsync(nameof(IT_Form.Models.StandardSoftware), "0", "Read", "Viewed Software Inventory and Analytics");

            return ProcessResult<StandardSoftwareIndexPageViewModel>.Success(analytics);
        }
    }

    public sealed class GetStandardSoftwareCreatePageQueryHandler
        : IRequestHandler<GetStandardSoftwareCreatePageQuery, ProcessResult<StandardSoftwareEditorPageViewModel>>
    {
        public Task<ProcessResult<StandardSoftwareEditorPageViewModel>> Handle(GetStandardSoftwareCreatePageQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessResult<StandardSoftwareEditorPageViewModel>.Success(
                request.Model ?? new StandardSoftwareEditorPageViewModel()));
        }
    }

    public sealed class GetStandardSoftwareEditPageQueryHandler
        : IRequestHandler<GetStandardSoftwareEditPageQuery, ProcessResult<StandardSoftwareEditorPageViewModel>>
    {
        private readonly ApplicationDbContext _context;

        public GetStandardSoftwareEditPageQueryHandler(ApplicationDbContext context) => _context = context;

        public async Task<ProcessResult<StandardSoftwareEditorPageViewModel>> Handle(GetStandardSoftwareEditPageQuery request, CancellationToken cancellationToken)
        {
            var model = request.Model ?? await _context.StandardSoftware
                .AsNoTracking()
                .Where(software => software.Id == request.Id)
                .ProjectToType<StandardSoftwareEditorPageViewModel>()
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
            {
                return ProcessResult<StandardSoftwareEditorPageViewModel>.NotFound();
            }

            return ProcessResult<StandardSoftwareEditorPageViewModel>.Success(model);
        }
    }
}
