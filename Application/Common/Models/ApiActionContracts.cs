// Project comment: ApiActionContracts.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Application.Common.Models
{
    /// <summary>
    /// Defines the ApiMutationResponse record used by the IT Form application.
    /// </summary>
    public sealed record ApiMutationResponse(
        bool Success,
        string Message,
        int RequestId = 0);

    /// <summary>
    /// Defines the OnboardingSignatureApiRequest class used by the IT Form application.
    /// </summary>
    public sealed class OnboardingSignatureApiRequest
    {
        [Required]
        public string SignatureType { get; set; } = string.Empty;

        [Required]
        public string SignatureData { get; set; } = string.Empty;
    }

    /// <summary>
    /// Defines the SignatureBase64ApiRequest class used by the IT Form application.
    /// </summary>
    public sealed class SignatureBase64ApiRequest
    {
        [Required]
        public string SignatureBase64 { get; set; } = string.Empty;
    }

    /// <summary>
    /// Defines the RejectionApiRequest class used by the IT Form application.
    /// </summary>
    public sealed class RejectionApiRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
