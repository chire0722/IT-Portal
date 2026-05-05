// Project comment: CreateCompanyCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Services;
using Mapster;
using Microsoft.AspNetCore.Http;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = IT_Form.Models.Company;

namespace IT_Form.Application.Features.Company.Writes.CreateCompany;

/// <summary>
/// Handles the CreateCompanyCommand command workflow.
/// </summary>
public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly IQuotaControl _quotaControl;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public CreateCompanyCommandHandler(
        ApplicationDbContext context,
        IWebHostEnvironment hostEnvironment,
        IQuotaControl? quotaControl = null,
        ICurrentUserAccessor? currentUserAccessor = null)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
        _quotaControl = quotaControl ?? new QuotaControl(context);
        _currentUserAccessor = currentUserAccessor ?? new CurrentUserAccessor(new HttpContextAccessor());
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        await _quotaControl.EnsureAvailableAsync(QuotaControl.Companies, user: _currentUserAccessor.User, cancellationToken: cancellationToken);

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
            var extension = Path.GetExtension(request.Logo.FileName).ToLowerInvariant();
            var fileName = $"{entity.Id}_Logo_{Guid.NewGuid():N}{extension}";
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
