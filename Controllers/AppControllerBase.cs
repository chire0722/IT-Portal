using FluentValidation;
using IT_Form.Application.Common.Models;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    public abstract class AppControllerBase : Controller
    {
        protected bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        protected string GetValidationErrorMessage(ValidationException ex)
        {
            ModelState.AddFluentValidationErrors(ex);
            return GetFirstModelStateErrorMessage();
        }

        protected string GetFirstModelStateErrorMessage()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Validation failed.";
        }

        protected IActionResult AjaxErrorOrRedirect(string message, string actionName, object routeValues)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message });

            TempData["ErrorMessage"] = message;
            return RedirectToAction(actionName, routeValues);
        }

        protected IActionResult AjaxSuccessOrRedirect(string message, string actionName, object routeValues)
        {
            if (IsAjaxRequest())
                return Json(new { success = true, message });

            TempData["SuccessMessage"] = message;
            return RedirectToAction(actionName, routeValues);
        }

        protected IActionResult RedirectWithError(string message, string actionName, object routeValues)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(actionName, routeValues);
        }

        protected IActionResult RedirectWithSuccess(
            string message,
            string actionName,
            object routeValues,
            string? warningMessage = null,
            string? autoOpenEmail = null,
            bool showItSummary = false)
        {
            TempData["SuccessMessage"] = message;

            if (!string.IsNullOrWhiteSpace(warningMessage))
                TempData["WarningMessage"] = warningMessage;

            if (!string.IsNullOrWhiteSpace(autoOpenEmail))
                TempData["AutoOpenEmail"] = autoOpenEmail;

            if (showItSummary)
                TempData["ShowITSummary"] = true;

            return RedirectToAction(actionName, routeValues);
        }

        protected IActionResult ProcessResponse(
            CommandResponse result,
            string actionName,
            int fallbackRequestId,
            bool ajaxAware = true,
            string? autoOpenEmail = null)
        {
            if (result.IsNotFound)
                return NotFound();

            if (result.IsForbidden)
                return Forbid();

            var requestId = result.RequestId > 0 ? result.RequestId : fallbackRequestId;
            if (result.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(autoOpenEmail))
                    TempData["AutoOpenEmail"] = autoOpenEmail;

                return ajaxAware
                    ? AjaxSuccessOrRedirect(result.Message, actionName, new { id = requestId })
                    : RedirectWithSuccess(result.Message, actionName, new { id = requestId });
            }

            return ajaxAware
                ? AjaxErrorOrRedirect(result.Message, actionName, new { id = requestId })
                : RedirectWithError(result.Message, actionName, new { id = requestId });
        }

        protected async Task<IActionResult> ExecuteWithValidationRedirect(
            Func<Task<IActionResult>> action,
            string actionName,
            object routeValues,
            bool ajaxAware = false)
        {
            try
            {
                return await action();
            }
            catch (ValidationException ex)
            {
                var message = GetValidationErrorMessage(ex);
                return ajaxAware
                    ? AjaxErrorOrRedirect(message, actionName, routeValues)
                    : RedirectWithError(message, actionName, routeValues);
            }
        }

        protected async Task<IActionResult> ExecuteWithValidationView<TViewModel>(
            Func<Task<IActionResult>> action,
            Func<Task<TViewModel>> buildViewModel)
        {
            try
            {
                return await action();
            }
            catch (ValidationException ex)
            {
                GetValidationErrorMessage(ex);
                return View(await buildViewModel());
            }
        }

        protected async Task<IActionResult> ExecuteWithValidationResult(
            Func<Task<IActionResult>> action,
            Func<Task<IActionResult>> onValidationFailure)
        {
            try
            {
                return await action();
            }
            catch (ValidationException ex)
            {
                GetValidationErrorMessage(ex);
                return await onValidationFailure();
            }
        }

        protected IActionResult HandleProcessResult<T>(ProcessResult<T> result)
        {
            if (result.IsNotFound)
                return NotFound();

            if (result.IsForbidden)
                return Forbid();

            if (!string.IsNullOrWhiteSpace(result.WarningMessage))
                TempData["WarningMessage"] = result.WarningMessage;

            if (!string.IsNullOrWhiteSpace(result.AutoOpenEmail))
                TempData["AutoOpenEmail"] = result.AutoOpenEmail;

            if (result.ShowItSummary)
                TempData["ShowITSummary"] = true;

            if (result.Data is IActionResult actionResult)
                return actionResult;

            if (result.Data is BinaryFilePayload filePayload)
                return File(filePayload.Content, filePayload.ContentType, filePayload.FileDownloadName);

            if (!string.IsNullOrWhiteSpace(result.RedirectAction))
            {
                if (result.AjaxAware && IsAjaxRequest())
                    return Json(new { success = result.IsSuccess, message = result.Message });

                if (!string.IsNullOrWhiteSpace(result.Message))
                    TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] = result.Message;

                return result.RequestId > 0
                    ? RedirectToAction(result.RedirectAction, new { id = result.RequestId })
                    : RedirectToAction(result.RedirectAction);
            }

            if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Message))
                TempData["ErrorMessage"] = result.Message;

            return View(result.Data);
        }

        protected IActionResult HandleJsonProcessResult<T>(ProcessResult<T> result)
        {
            if (result.IsNotFound)
                return NotFound();

            if (result.IsForbidden)
                return Forbid();

            if (!result.IsSuccess)
                return string.IsNullOrWhiteSpace(result.Message)
                    ? BadRequest()
                    : BadRequest(result.Message);

            return Json(result.Data);
        }
    }
}
