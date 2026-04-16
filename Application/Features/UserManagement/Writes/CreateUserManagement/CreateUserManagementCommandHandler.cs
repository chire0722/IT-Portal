using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Common.Validation;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement;

public sealed class CreateUserManagementCommandHandler : IRequestHandler<CreateUserManagementCommand, string>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IEmailBackgroundQueue _emailBackgroundQueue;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreateUserManagementCommandHandler> _logger;

    public CreateUserManagementCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ICurrentUserAccessor currentUserAccessor,
        IEmailBackgroundQueue emailBackgroundQueue,
        IConfiguration configuration,
        ILogger<CreateUserManagementCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _currentUserAccessor = currentUserAccessor;
        _emailBackgroundQueue = emailBackgroundQueue;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> Handle(CreateUserManagementCommand request, CancellationToken cancellationToken)
    {
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanies, cancellationToken);

        var user = request.Adapt<ApplicationUser>();
        user.MustChangePassword = true;

        var result = await _userManager.CreateAsync(user, request.Password!);
        result.EnsureSucceeded();

        await UserManagementRules.AssignTitlesAsync(_context, user.Id, request.IsIT, request.IsHR, request.IsHOD);
        UserManagementRules.AssignCompanies(_context, user.Id, request.SelectedCompanies);
        await QueueWelcomeEmailAsync(user, request.Password!, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("IT admin created user {Username} with titles.", request.Username);
        return user.Id;
    }

    private async Task QueueWelcomeEmailAsync(ApplicationUser user, string temporaryPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var encodedName = WebUtility.HtmlEncode(user.FullName);
        var encodedUsername = WebUtility.HtmlEncode(user.UserName ?? string.Empty);
        var encodedPassword = WebUtility.HtmlEncode(temporaryPassword);
        var paramsRows = $@"
            <tr class='params-row'>
                <td class='params-label' style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Full Name</td>
                <td class='params-value' style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{encodedName}</td>
            </tr>
            <tr class='params-row'>
                <td class='params-label' style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Username</td>
                <td class='params-value' style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{encodedUsername}</td>
            </tr>
            <tr class='params-row'>
                <td class='params-label' style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Temporary Password</td>
                <td class='params-value' style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{encodedPassword}</td>
            </tr>
            <tr class='params-row'>
                <td class='params-label' style='padding: 14px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Important</td>
                <td class='params-value' style='padding: 14px 15px; font-size: 14px; color: #475569;'>You will be required to change this password after your first login.</td>
            </tr>";

        var appDomainSetting = _configuration["AppDomain"] ?? "http://localhost:5063";
        var domains = appDomainSetting.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var baseDomain = domains.FirstOrDefault(domain => !domain.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            ?? domains.FirstOrDefault()
            ?? "http://localhost:5063";
        var loginUrl = $"{baseDomain.TrimEnd('/')}/Account/Login";

        var body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
            "Welcome to IT Portal",
            $"Hello <b>{encodedName}</b>,<br /><br />Your system user account has been created successfully. Please use the credentials below to sign in to the portal.",
            paramsRows,
            loginUrl,
            "Open IT Portal");

        await _emailBackgroundQueue.QueueEmailAsync(
            new RenderedEmailMessage(
                user.Email,
                "Welcome to IT Portal - Your Account Details",
                body),
            cancellationToken);
    }

    private async Task EnsureRequestedCompaniesAreInScopeAsync(
        IEnumerable<string>? selectedCompanies,
        CancellationToken cancellationToken)
    {
        var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
        if (scopedCompanies.Count == 0)
        {
            return;
        }

        var requestedCompanies = selectedCompanies?
            .Where(company => !string.IsNullOrWhiteSpace(company))
            .ToList() ?? [];

        if (!CompanyScopeHelper.AreAllCompaniesAllowed(scopedCompanies, requestedCompanies))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only assign users to your own company scope.")]);
        }

        var knownCompanies = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyDisplayNamesAsync(_context, _currentUserAccessor.User, cancellationToken);
        if (!requestedCompanies.All(company => knownCompanies.Any(knownCompany => CompanyHelper.IsMatch(knownCompany, company))))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "One or more selected companies are not available in your company scope.")]);
        }
    }
}
