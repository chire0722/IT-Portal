// Project comment: CreateCompanyCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Helpers;
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
    private readonly IFileMalwareScanner? _malwareScanner;

    public CreateCompanyCommandHandler(
        ApplicationDbContext context,
        IWebHostEnvironment hostEnvironment,
        IQuotaControl? quotaControl = null,
        ICurrentUserAccessor? currentUserAccessor = null,
        IFileMalwareScanner? malwareScanner = null)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
        _quotaControl = quotaControl ?? new QuotaControl(context);
        _currentUserAccessor = currentUserAccessor ?? new CurrentUserAccessor(new HttpContextAccessor());
        _malwareScanner = malwareScanner;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        await MalwareScanHelper.EnsureCleanAsync(request.Logo, _malwareScanner, "Company logo", cancellationToken);

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
        var newLogoPath = await CompanyLogoStorage.SaveAsync(_hostEnvironment, request.Logo, cancellationToken);
        if (!string.IsNullOrWhiteSpace(newLogoPath))
        {
            entity.LogoPath = newLogoPath;
        }

        _context.Companies.Add(entity);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            CompanyLogoStorage.DeleteIfManaged(_hostEnvironment, newLogoPath);
            throw;
        }

        return entity.Id;
    }
}
