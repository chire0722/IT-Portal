// Project comment: ProcessResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Models
{
    /// <summary>
    /// Represents the result returned by the Process workflow.
    /// </summary>
    public sealed record ProcessResult<T>
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public string? RedirectAction { get; init; }
        public int RequestId { get; init; }
        public bool IsNotFound { get; init; }
        public bool IsForbidden { get; init; }
        public bool AjaxAware { get; init; }
        public string? AutoOpenEmail { get; init; }
        public string? WarningMessage { get; init; }
        public bool ShowItSummary { get; init; }

        /// <summary>
        /// Runs the Success operation.
        /// </summary>
        public static ProcessResult<T> Success(T? data = default, string message = "", string? redirectAction = null, int requestId = 0) =>
            new() { IsSuccess = true, Data = data, Message = message, RedirectAction = redirectAction, RequestId = requestId };

        /// <summary>
        /// Runs the Failure operation.
        /// </summary>
        public static ProcessResult<T> Failure(string message, string? redirectAction = null, int requestId = 0, bool ajaxAware = false) =>
            new() { Message = message, RedirectAction = redirectAction, RequestId = requestId, AjaxAware = ajaxAware };

        /// <summary>
        /// Runs the NotFound operation.
        /// </summary>
        public static ProcessResult<T> NotFound() => new() { IsNotFound = true };

        /// <summary>
        /// Runs the Forbidden operation.
        /// </summary>
        public static ProcessResult<T> Forbidden() => new() { IsForbidden = true };
    }
}
