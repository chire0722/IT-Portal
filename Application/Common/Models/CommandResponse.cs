// Project comment: CommandResponse.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Models
{
    /// <summary>
    /// Defines the CommandResponse class used by the IT Form application.
    /// </summary>
    public sealed class CommandResponse
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = string.Empty;
        public int RequestId { get; init; }
        public bool IsNotFound { get; init; }
        public bool IsForbidden { get; init; }
    }
}
