// Project comment: QuotaReadOnlyFilter.cs blocks MVC mutation pages while quota areas are read-only.

using FluentValidation;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IT_Form.Filters;

public sealed class QuotaReadOnlyFilter : IAsyncActionFilter
{
    private static readonly string[] ReadOnlyActionPrefixes =
    [
        "Index",
        "Details",
        "Report",
        "Members",
        "Export",
        "Download",
        "Get"
    ];

    private readonly IQuotaControl _quotaControl;
    private readonly IMaintenanceModeService _maintenanceModeService;

    public QuotaReadOnlyFilter(IQuotaControl quotaControl, IMaintenanceModeService maintenanceModeService)
    {
        _quotaControl = quotaControl;
        _maintenanceModeService = maintenanceModeService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (IsReadOnlyAction(context) || IsMaintenanceExcluded(context))
        {
            await next();
            return;
        }

        try
        {
            await _maintenanceModeService.EnsureWritableAsync(context.HttpContext.User, context.HttpContext.RequestAborted);
            var quotaKey = ResolveQuotaKey(context);
            if (quotaKey != null)
            {
                await _quotaControl.EnsureWritableAsync(quotaKey, context.HttpContext.User, context.HttpContext.RequestAborted);
            }
        }
        catch (ValidationException ex)
        {
            var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? QuotaControl.ReadOnlyMessage;
            if (HttpMethods.IsGet(context.HttpContext.Request.Method) && context.Controller is Controller controller)
            {
                controller.TempData["ErrorMessage"] = message;
                context.Result = new RedirectToActionResult("Index", null, null);
                return;
            }

            context.Result = new BadRequestObjectResult(new { success = false, message });
            return;
        }

        await next();
    }

    private static bool IsReadOnlyAction(ActionExecutingContext context)
    {
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) ||
            HttpMethods.IsHead(context.HttpContext.Request.Method))
        {
            return !IsMutationFormAction(action);
        }

        return ReadOnlyActionPrefixes.Any(prefix => action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMutationFormAction(string action)
        => action.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
           action.StartsWith("Edit", StringComparison.OrdinalIgnoreCase) ||
           action.StartsWith("Delete", StringComparison.OrdinalIgnoreCase);

    private static bool IsMaintenanceExcluded(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        return string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(controller, "Verification", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveQuotaKey(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;

        return controller switch
        {
            "ITAsset" => QuotaControl.Assets,
            "Company" => QuotaControl.Companies,
            "Department" => QuotaControl.Departments,
            "EmailGroup" => QuotaControl.EmailGroups,
            "SharedMailbox" => QuotaControl.SharedMailboxes,
            "ServerFolder" => QuotaControl.ServerFolders,
            "UserManagement" => QuotaControl.Users,
            "StandardSoftware" => ResolveStandardSoftwareQuotaKey(action),
            _ => null
        };
    }

    private static string ResolveStandardSoftwareQuotaKey(string action)
        => action.Contains("License", StringComparison.OrdinalIgnoreCase) ||
           action.Contains("Key", StringComparison.OrdinalIgnoreCase)
            ? QuotaControl.SoftwareVersions
            : QuotaControl.Software;
}
