// Project comment: AppControllerBase.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Application.Common.Models;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    /// <summary>
    /// Defines the AppControllerBase class used by the IT Form application.
    /// </summary>
    public abstract class AppControllerBase : Controller
    {
        /// <summary>
        /// Runs the IsAjaxRequest operation.
        /// </summary>
        protected bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        /// <summary>
        /// Gets the data required by the GetValidationErrorMessage operation.
        /// </summary>
        protected string GetValidationErrorMessage(ValidationException ex)
        {
            ModelState.AddFluentValidationErrors(ex);
            var message = GetFirstModelStateErrorMessage();
            AddModelLevelError(message);
            TempData["ErrorMessage"] = message;
            return message;
        }

        /// <summary>
        /// Gets the data required by the GetFirstModelStateErrorMessage operation.
        /// </summary>
        protected string GetFirstModelStateErrorMessage()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Validation failed.";
        }

        /// <summary>
        /// Runs the AjaxErrorOrRedirect operation.
        /// </summary>
        protected IActionResult AjaxErrorOrRedirect(string message, string actionName, object routeValues)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message });

            TempData["ErrorMessage"] = message;
            return RedirectToAction(actionName, routeValues);
        }

        /// <summary>
        /// Runs the AjaxSuccessOrRedirect operation.
        /// </summary>
        protected IActionResult AjaxSuccessOrRedirect(string message, string actionName, object routeValues)
        {
            if (IsAjaxRequest())
                return Json(new { success = true, message });

            TempData["SuccessMessage"] = message;
            return RedirectToAction(actionName, routeValues);
        }

        /// <summary>
        /// Runs the RedirectWithError operation.
        /// </summary>
        protected IActionResult RedirectWithError(string message, string actionName, object routeValues)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(actionName, routeValues);
        }

        /// <summary>
        /// Runs the RedirectWithSuccess operation.
        /// </summary>
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

        /// <summary>
        /// Processes data for the ProcessResponse operation.
        /// </summary>
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

        /// <summary>
        /// Runs the ExecuteWithValidationRedirect operation.
        /// </summary>
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

        /// <summary>
        /// Runs the ExecuteWithValidationResult operation.
        /// </summary>
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
            {
                AddModelLevelError(result.Message);
                TempData["ErrorMessage"] = result.Message;
            }

            return View(result.Data);
        }

        /// <summary>
        /// Runs the AddModelLevelError operation.
        /// </summary>
        private void AddModelLevelError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var existingMessages = ModelState.TryGetValue(string.Empty, out var entry)
                ? entry.Errors.Select(error => error.ErrorMessage)
                : Enumerable.Empty<string>();

            if (!existingMessages.Contains(message, StringComparer.Ordinal))
            {
                ModelState.AddModelError(string.Empty, message);
            }
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
