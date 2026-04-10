using FluentValidation;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IT_Form.Filters
{
    public sealed class ValidationExceptionFilter : IAsyncExceptionFilter
    {
        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.Exception is not ValidationException validationException)
            {
                return Task.CompletedTask;
            }

            context.ModelState.AddFluentValidationErrors(validationException);

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
                var firstError = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Validation failed.";

                context.Result = new JsonResult(new { success = false, message = firstError })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            else
            {
                context.Result = new ViewResult
                {
                    ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                        new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                        context.ModelState)
                };
            }

            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }

        private static bool IsApiRequest(ExceptionContext context)
            => context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }
}
