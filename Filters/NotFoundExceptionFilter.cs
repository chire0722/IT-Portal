// Project comment: NotFoundExceptionFilter.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IT_Form.Filters
{
    /// <summary>
    /// Defines the NotFoundExceptionFilter class used by the IT Form application.
    /// </summary>
    public sealed class NotFoundExceptionFilter : IAsyncExceptionFilter
    {
        /// <summary>
        /// Runs the OnExceptionAsync operation.
        /// </summary>
        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.Exception is not NotFoundException notFoundException)
            {
                return Task.CompletedTask;
            }

            if (IsApiRequest(context))
            {
                context.Result = new NotFoundObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = notFoundException.Message
                });
            }
            else
            {
                context.Result = new NotFoundObjectResult(notFoundException.Message);
            }

            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Runs the IsApiRequest operation.
        /// </summary>
        private static bool IsApiRequest(ExceptionContext context)
            => context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }
}
