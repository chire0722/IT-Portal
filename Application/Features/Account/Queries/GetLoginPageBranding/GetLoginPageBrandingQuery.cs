// Project comment: GetLoginPageBrandingQuery.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IT_Form.Application.Features.Account.Queries.GetLoginPageBranding;

/// <summary>
/// Carries data needed to execute the GetLoginPageBranding query.
/// </summary>
public sealed record GetLoginPageBrandingQuery(
    LoginViewModel? Form = null,
    string? LockoutEndTimeIso = null) : IRequest<LoginPageBrandingViewModel>;

/// <summary>
/// Handles the GetLoginPageBrandingQuery query workflow.
/// </summary>
public sealed class GetLoginPageBrandingQueryHandler : IRequestHandler<GetLoginPageBrandingQuery, LoginPageBrandingViewModel>
{
    private readonly ApplicationDbContext _context;
    private readonly LoginPageBrandingOptions _options;

    public GetLoginPageBrandingQueryHandler(
        ApplicationDbContext context,
        IOptions<LoginPageBrandingOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<LoginPageBrandingViewModel> Handle(
        GetLoginPageBrandingQuery request,
        CancellationToken cancellationToken)
    {
        var selectedLogoPath = await _context.Companies
            .AsNoTracking()
            .Where(c => c.IsActive && c.IsLoginPageLogo && !string.IsNullOrEmpty(c.LogoPath))
            .OrderBy(c => c.Name)
            .Select(c => c.LogoPath)
            .FirstOrDefaultAsync(cancellationToken);

        var model = new LoginPageBrandingViewModel
        {
            Username = request.Form?.Username ?? string.Empty,
            Password = request.Form?.Password ?? string.Empty,
            RememberMe = request.Form?.RememberMe ?? false,
            CompanyName = _options.CompanyName,
            SystemName = _options.SystemName,
            Subtitle = _options.Subtitle,
            BackgroundImagePath = _options.BackgroundImagePath,
            LoginLogoPath = selectedLogoPath,
            SystemVersion = string.IsNullOrWhiteSpace(_options.SystemVersion)
                ? typeof(GetLoginPageBrandingQueryHandler).Assembly.GetName().Version?.ToString() ?? "dev"
                : _options.SystemVersion,
            LockoutEndTimeIso = request.LockoutEndTimeIso
        };

        return model;
    }
}
