namespace IT_Form.Application.Common.Models
{
    public sealed class CommandResponse
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = string.Empty;
        public int RequestId { get; init; }
        public bool IsNotFound { get; init; }
        public bool IsForbidden { get; init; }
    }
}
