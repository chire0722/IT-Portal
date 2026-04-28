// Project comment: IOutlookService.cs is part of the IT Form application source code.

namespace IT_Form.Services
{
    /// <summary>
    /// Provides IOutlook service behavior for the application.
    /// </summary>
    public interface IOutlookService
    {
        void DisplayEmail(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null, string cc = "", string? signatureBase64 = null, bool bypassActiveCheck = false);
    }
}
