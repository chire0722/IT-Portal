// Project comment: ValidationExceptionFilter.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IT_Form.Filters
{
    /// <summary>
    /// Defines the ValidationExceptionFilter class used by the IT Form application.
    /// </summary>
    public sealed class ValidationExceptionFilter : IAsyncExceptionFilter
    {
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;

        public ValidationExceptionFilter(ITempDataDictionaryFactory tempDataDictionaryFactory)
        {
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
        }

        /// <summary>
        /// Runs the OnExceptionAsync operation.
        /// </summary>
        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.Exception is not ValidationException validationException)
            {
                return Task.CompletedTask;
            }

            context.ModelState.AddFluentValidationErrors(validationException);
            var firstError = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Validation failed.";

            if (!HasModelLevelError(context, firstError))
            {
                context.ModelState.AddModelError(string.Empty, firstError);
            }

            _tempDataDictionaryFactory.GetTempData(context.HttpContext)["ErrorMessage"] = firstError;

            if (IsApiRequest(context))
            {
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed."
                });
            }
            else if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Result = new JsonResult(new { success = false, message = firstError })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            else
            {
                context.Result = new ViewResult
                {
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState),
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Runs the HasModelLevelError operation.
        /// </summary>
        private static bool HasModelLevelError(ExceptionContext context, string message)
            => context.ModelState.TryGetValue(string.Empty, out var entry) &&
               entry.Errors.Any(error => string.Equals(error.ErrorMessage, message, StringComparison.Ordinal));

        /// <summary>
        /// Runs the IsApiRequest operation.
        /// </summary>
        private static bool IsApiRequest(ExceptionContext context)
            => context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }
}
