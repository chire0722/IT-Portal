using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = IT_Form.Models.Company;

namespace IT_Form.Application.Features.Company.Writes.CreateCompany;

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public CreateCompanyCommandHandler(
        ApplicationDbContext context,
        IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<int> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (request.IsLoginPageLogo)
        {
            var companiesWithLoginLogo = await _context.Companies
                .Where(company => company.IsLoginPageLogo)
                .ToListAsync(cancellationToken);

            foreach (var company in companiesWithLoginLogo)
            {
                company.IsLoginPageLogo = false;
            }
        }

        var entity = request.Adapt<CompanyEntity>();

        _context.Companies.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        if (request.Logo != null && request.Logo.Length > 0)
        {
            var fileName = entity.Id + "_Logo_" + Path.GetFileName(request.Logo.FileName);
            var relativePath = Path.Combine("uploads", "companies", "logos", fileName).Replace("\\", "/");
            var directoryPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "companies", "logos");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var fullPath = Path.Combine(directoryPath, fileName);
            await using var fileStream = new FileStream(fullPath, FileMode.Create);
            await request.Logo.CopyToAsync(fileStream, cancellationToken);

            entity.LogoPath = relativePath;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return entity.Id;
    }
}
