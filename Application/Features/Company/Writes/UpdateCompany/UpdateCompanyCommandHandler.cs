// Project comment: UpdateCompanyCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Services;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = IT_Form.Models.Company;

namespace IT_Form.Application.Features.Company.Writes.UpdateCompany;

/// <summary>
/// Handles the UpdateCompanyCommand command workflow.
/// </summary>
public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ICompanyReferenceSyncService _companyReferenceSyncService;
    private readonly IFileMalwareScanner? _malwareScanner;

    public UpdateCompanyCommandHandler(
        ApplicationDbContext context,
        IWebHostEnvironment hostEnvironment,
        ICompanyReferenceSyncService companyReferenceSyncService,
        IFileMalwareScanner? malwareScanner = null)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
        _companyReferenceSyncService = companyReferenceSyncService;
        _malwareScanner = malwareScanner;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Companies
            .FirstOrDefaultAsync(company => company.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CompanyEntity), request.Id);

        await MalwareScanHelper.EnsureCleanAsync(request.Logo, _malwareScanner, "Company logo", cancellationToken);


        var originalCompany = new CompanyEntity
        {
            Id = entity.Id,
            Name = entity.Name,
            Lot = entity.Lot,
            LogoPath = entity.LogoPath,
            IsLoginPageLogo = entity.IsLoginPageLogo,
            IsActive = entity.IsActive,
            TimeZoneId = entity.TimeZoneId,
            BusinessStartTime = entity.BusinessStartTime,
            BusinessEndTime = entity.BusinessEndTime,
            WorksOnMonday = entity.WorksOnMonday,
            WorksOnTuesday = entity.WorksOnTuesday,
            WorksOnWednesday = entity.WorksOnWednesday,
            WorksOnThursday = entity.WorksOnThursday,
            WorksOnFriday = entity.WorksOnFriday,
            WorksOnSaturday = entity.WorksOnSaturday,
            WorksOnSunday = entity.WorksOnSunday
        };

        var newLogoPath = await CompanyLogoStorage.SaveAsync(_hostEnvironment, request.Logo, cancellationToken);
        request.Adapt(entity);

        if (!string.IsNullOrWhiteSpace(newLogoPath))
        {
            entity.LogoPath = newLogoPath;
        }

        if (entity.IsLoginPageLogo)
        {
            var companiesWithLoginLogo = await _context.Companies
                .Where(company => company.IsLoginPageLogo && company.Id != entity.Id)
                .ToListAsync(cancellationToken);

            foreach (var company in companiesWithLoginLogo)
            {
                company.IsLoginPageLogo = false;
            }
        }

        int syncedCount;
        try
        {
            syncedCount = await _companyReferenceSyncService.ApplyUpdatedCompanySyncAsync(originalCompany, entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            CompanyLogoStorage.DeleteIfManaged(_hostEnvironment, newLogoPath);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(newLogoPath)
            && !string.Equals(originalCompany.LogoPath, newLogoPath, StringComparison.Ordinal))
        {
            CompanyLogoStorage.DeleteIfManaged(_hostEnvironment, originalCompany.LogoPath);
        }

        return syncedCount;
    }
}
