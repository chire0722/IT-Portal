// Project comment: ErrorViewModel.cs is part of the IT Form application source code.

namespace IT_Form.Models
{
    /// <summary>
    /// Provides view data for the Error screen or form.
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
