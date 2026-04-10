using IT_Form.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace IT_Form.Services
{
    public class OutlookService : IOutlookService
    {
        private readonly ILogger<OutlookService> _logger;
        private readonly IT_Form.Models.SmtpSettings _smtpSettings;
        private readonly IServiceScopeFactory _scopeFactory;

        public OutlookService(
            ILogger<OutlookService> logger,
            Microsoft.Extensions.Options.IOptions<IT_Form.Models.SmtpSettings> smtpSettings,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _smtpSettings = smtpSettings.Value;
            _scopeFactory = scopeFactory;
        }

        public void DisplayEmail(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null, string cc = "", string? signatureBase64 = null, bool bypassActiveCheck = false)
        {
            try
            {
                _logger.LogInformation("Attempting to send email. Original To: {To}, CC: {Cc}, BypassActiveCheck: {Bypass}", to, cc, bypassActiveCheck);

                if (string.IsNullOrWhiteSpace(_smtpSettings.Username) || 
                    string.IsNullOrWhiteSpace(_smtpSettings.Password) || 
                    _smtpSettings.Password.Contains("SET_VIA_ENV_VAR"))
                {
                    throw new InvalidOperationException("SMTP Credentials are not fully configured. Please set the 'IT_FORM__SmtpSettings__Password' machine environment variable.");
                }

                // Retrieve deactivated user emails from the database ONLY if NOT bypassing the check
                List<string> deactivatedEmails = new();
                if (!bypassActiveCheck)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        // Fetch normalized emails to ensure case-insensitive matching
                        deactivatedEmails = dbContext.Users
                            .Where(u => !u.IsActive && !string.IsNullOrEmpty(u.Email))
                            .Select(u => u.Email!.ToLower())
                            .ToList();
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogWarning(dbEx, "Failed to fetch deactivated users. Email filtering may be incomplete.");
                    }
                }

                using var message = new System.Net.Mail.MailMessage();
                message.From = new System.Net.Mail.MailAddress(_smtpSettings.Username, _smtpSettings.FromName);

                // Handle multiple recipients if separated by semicolon or comma and filter deactivated
                if (!string.IsNullOrWhiteSpace(to))
                {
                    var recipients = to.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var recipient in recipients)
                    {
                        var addr = recipient.Trim();
                        if (!string.IsNullOrEmpty(addr))
                        {
                            if (!bypassActiveCheck && deactivatedEmails.Contains(addr.ToLower()))
                            {
                                _logger.LogInformation("Dropped deactivated user from TO list: {Email}", addr);
                            }
                            else
                            {
                                message.To.Add(addr);
                            }
                        }
                    }
                }

                // If To address list ended up empty, fallback to IT department to avoid crashes
                if (message.To.Count == 0)
                {
                    message.To.Add("itdept@ytcprecast.com");
                    _logger.LogInformation("All TO addresses were dropped or empty. Falling back to IT Dept.");
                }

                if (!string.IsNullOrWhiteSpace(cc))
                {
                    var ccRecipients = cc.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var ccRecipient in ccRecipients)
                    {
                        var addr = ccRecipient.Trim();
                        if (!string.IsNullOrEmpty(addr))
                        {
                            if (!bypassActiveCheck && deactivatedEmails.Contains(addr.ToLower()))
                            {
                                _logger.LogInformation("Dropped deactivated user from CC list: {Email}", addr);
                            }
                            else
                            {
                                message.CC.Add(addr);
                            }
                        }
                    }
                }

                message.Subject = subject;
                if (attachment != null && !string.IsNullOrEmpty(attachmentName))
                {
                    message.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(attachment), attachmentName));
                }

                // Handle Signature CID Embedding
                if (!string.IsNullOrWhiteSpace(signatureBase64) && signatureBase64.Contains("base64,"))
                {
                    try
                    {
                        var parts = signatureBase64.Split(',');
                        var header = parts[0];
                        var base64Data = parts[1];
                        var contentType = header.Split(':')[1].Split(';')[0];
                        var imageBytes = System.Convert.FromBase64String(base64Data);

                        // Plain text fallback (extract from HTML or use generic)
                        string plainText = "Please view this email in an HTML-enabled client.";
                        var plainView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain");
                        message.AlternateViews.Add(plainView);

                        // HTML view with linked resource
                        var htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

                        var imageStream = new MemoryStream(imageBytes);
                        var signatureResource = new System.Net.Mail.LinkedResource(imageStream, contentType);
                        signatureResource.ContentId = "signature";
                        signatureResource.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;

                        htmlView.LinkedResources.Add(signatureResource);
                        message.AlternateViews.Add(htmlView);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to embed signature CID. Falling back to simple HTML body.");
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                    }
                }
                else
                {
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;
                }

                using var client = new System.Net.Mail.SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    Credentials = new System.Net.NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                // Send email synchronously to replace the previous Display() COM behavior
                client.Send(message);
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP.");
                throw; // Preserve the throw to match previous behaviour
            }
        }
    }
}
