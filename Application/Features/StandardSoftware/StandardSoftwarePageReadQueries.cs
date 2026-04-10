using IT_Form.Application.Common.Models;
using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware
{
    public sealed record GetStandardSoftwareIndexPageQuery(string? SearchString, bool? IsMandatory, bool? IsSubscription)
        : IRequest<ProcessResult<StandardSoftwareIndexPageViewModel>>;

    public sealed record GetStandardSoftwareCreatePageQuery(StandardSoftwareEditorPageViewModel? Model = null)
        : IRequest<ProcessResult<StandardSoftwareEditorPageViewModel>>;

    public sealed record GetStandardSoftwareEditPageQuery(int Id, StandardSoftwareEditorPageViewModel? Model = null)
        : IRequest<ProcessResult<StandardSoftwareEditorPageViewModel>>;

    public sealed class GetStandardSoftwareIndexPageQueryHandler
        : IRequestHandler<GetStandardSoftwareIndexPageQuery, ProcessResult<StandardSoftwareIndexPageViewModel>>
    {
        private readonly ApplicationDbContext _context;

        public GetStandardSoftwareIndexPageQueryHandler(ApplicationDbContext context) => _context = context;

        public async Task<ProcessResult<StandardSoftwareIndexPageViewModel>> Handle(GetStandardSoftwareIndexPageQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var softwareListQuery = _context.StandardSoftware
                .AsNoTracking()
                .AsQueryable();

            var analytics = new StandardSoftwareIndexPageViewModel
            {
                TotalSoftwareCount = await softwareListQuery.CountAsync(cancellationToken),
                TotalVersionCount = await _context.SoftwareLicenses
                    .AsNoTracking()
                    .CountAsync(cancellationToken),
                TotalSeatCapacity = await _context.SoftwareLicenses
                    .AsNoTracking()
                    .Where(license => !license.IsFree)
                    .SumAsync(license => (int?)license.ContractLimit, cancellationToken) ?? 0,
                TotalAssignedSeats = await _context.SoftwareKeys
                    .AsNoTracking()
                    .CountAsync(key => key.IsAssigned && key.SoftwareLicense != null && !key.SoftwareLicense.IsFree, cancellationToken),
                ExpiringSoonCount = await _context.SoftwareLicenses
                    .AsNoTracking()
                    .CountAsync(license => license.ExpiryDate.HasValue && license.ExpiryDate.Value <= today.AddDays(30), cancellationToken)
            };

            var query = softwareListQuery;

            if (!string.IsNullOrEmpty(request.SearchString))
            {
                var lowerSearch = request.SearchString.ToLower();
                query = query.Where(software => software.SoftwareName.ToLower().Contains(lowerSearch) ||
                                                software.Licenses.Any(license => license.SoftwareName.ToLower().Contains(lowerSearch)));
            }

            if (request.IsMandatory.HasValue)
            {
                query = query.Where(software => software.IsMandatory == request.IsMandatory.Value);
            }

            if (request.IsSubscription.HasValue)
            {
                query = query.Where(software => software.Licenses.Any(license => license.IsSubscription == request.IsSubscription.Value));
            }

            var filteredList = await query
                .OrderBy(software => software.SoftwareName)
                .ProjectToType<StandardSoftwareIndexItem>()
                .ToListAsync(cancellationToken);

            if (request.IsSubscription.HasValue)
            {
                foreach (var software in filteredList)
                {
                    software.Licenses = software.Licenses
                        .Where(license => license.IsSubscription == request.IsSubscription.Value)
                        .ToList();
                }
            }

            analytics.SoftwareList = filteredList;
            analytics.CurrentSearch = request.SearchString;
            analytics.IsMandatory = request.IsMandatory;
            analytics.IsSubscription = request.IsSubscription;

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
