// Project comment: AppApiControllerBase.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    [ApiController]
    /// <summary>
    /// Defines the AppApiControllerBase class used by the IT Form application.
    /// </summary>
    public abstract class AppApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Runs the HandleCommandResponse operation.
        /// </summary>
        protected IActionResult HandleCommandResponse(CommandResponse result, int successStatusCode = StatusCodes.Status200OK)
        {
            if (result.IsNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: string.IsNullOrWhiteSpace(result.Message) ? "Resource not found." : result.Message);
            }

            if (result.IsForbidden)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: string.IsNullOrWhiteSpace(result.Message) ? "You are not authorized to perform this action." : result.Message);
            }

            if (!result.IsSuccess)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Request Failed",
                    detail: string.IsNullOrWhiteSpace(result.Message) ? "The request could not be completed." : result.Message);
            }

            return StatusCode(successStatusCode, new ApiMutationResponse(true, result.Message, result.RequestId));
        }

        protected IActionResult HandleProcessResult<T>(ProcessResult<T> result, int successStatusCode = StatusCodes.Status200OK)
        {
            if (result.IsNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: string.IsNullOrWhiteSpace(result.Message) ? "Resource not found." : result.Message);
            }

            if (result.IsForbidden)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: string.IsNullOrWhiteSpace(result.Message) ? "You are not authorized to perform this action." : result.Message);
            }

            if (!result.IsSuccess)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Request Failed",
                    detail: string.IsNullOrWhiteSpace(result.Message) ? "The request could not be completed." : result.Message);
            }

            if (result.Data is not null)
            {
                return StatusCode(successStatusCode, result.Data);
            }

            return StatusCode(successStatusCode, new ApiMutationResponse(true, result.Message, result.RequestId));
        }
    }
}
