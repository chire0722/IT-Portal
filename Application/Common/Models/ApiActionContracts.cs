using System.ComponentModel.DataAnnotations;

namespace IT_Form.Application.Common.Models
{
    public sealed record ApiMutationResponse(
        bool Success,
        string Message,
        int RequestId = 0);

    public sealed class OnboardingSignatureApiRequest
    {
        [Required]
        public string SignatureType { get; set; } = string.Empty;

        [Required]
        public string SignatureData { get; set; } = string.Empty;
    }

    public sealed class SignatureBase64ApiRequest
    {
        [Required]
        public string SignatureBase64 { get; set; } = string.Empty;
    }

    public sealed class RejectionApiRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
