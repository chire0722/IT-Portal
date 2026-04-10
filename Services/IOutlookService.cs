namespace IT_Form.Services
{
    public interface IOutlookService
    {
        void DisplayEmail(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null, string cc = "", string? signatureBase64 = null, bool bypassActiveCheck = false);
    }
}
