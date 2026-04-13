using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Services;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = IT_Form.Models.Company;

namespace IT_Form.Application.Features.Company.Writes.UpdateCompany;

public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ICompanyReferenceSyncService _companyReferenceSyncService;

    public UpdateCompanyCommandHandler(
        ApplicationDbContext context,
        IWebHostEnvironment hostEnvironment,
        ICompanyReferenceSyncService companyReferenceSyncService)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
        _companyReferenceSyncService = companyReferenceSyncService;
    }

    public async Task<int> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Companies
            .FirstOrDefaultAsync(company => company.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CompanyEntity), request.Id);

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

        request.Adapt(entity);

        if (request.Logo != null && request.Logo.Length > 0)
        {
            var fileName = entity.Id + "_Logo_" + Path.GetFileName(request.Logo.FileName);
            var directoryPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "companies", "logos");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!string.IsNullOrEmpty(originalCompany.LogoPath))
            {
                var oldPath = Path.Combine(_hostEnvironment.WebRootPath, originalCompany.LogoPath.TrimStart('/'));
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }
            }

            var fullPath = Path.Combine(directoryPath, fileName);
            await using var fileStream = new FileStream(fullPath, FileMode.Create);
            await request.Logo.CopyToAsync(fileStream, cancellationToken);

            entity.LogoPath = Path.Combine("uploads", "companies", "logos", fileName).Replace("\\", "/");
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

        var syncedCount = await _companyReferenceSyncService.ApplyUpdatedCompanySyncAsync(originalCompany, entity);
        await _context.SaveChangesAsync(cancellationToken);

        return syncedCount;
    }
}
