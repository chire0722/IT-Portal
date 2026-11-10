// Project comment: NotificationController.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using IT_Form.Models;
using IT_Form.Models.Enums;
using System.Net.Mail;
using IT_Form.Helpers;
namespace IT_Form.Controllers
{
    [Authorize]
    /// <summary>
    /// Handles web requests for the Notification area.
    /// </summary>
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;
        private readonly IOutlookService _outlookService;
        private readonly ILogger<NotificationController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly IConfiguration _configuration;

        public NotificationController(
            ApplicationDbContext context,
            IPdfService pdfService,
            IOutlookService outlookService,
            ILogger<NotificationController> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ISignatureIntegrityService integrityService)
        {
            _context = context;
            _pdfService = pdfService;
            _outlookService = outlookService;
            _logger = logger;
            _userManager = userManager;
            _integrityService = integrityService;
            _configuration = configuration;
        }

        private async Task<(string? Name, string? Title, string? Image)> GetSenderInfoAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return (user?.FullName, user?.JobTitle, user?.EmailSignatureImage);
        }

        /// <summary>
        /// Gets the data required by the GetAppDomain operation.
        /// </summary>
        private string GetAppDomain()
        {
            var configuredDomain = _configuration["AppDomain"];
            if (!string.IsNullOrWhiteSpace(configuredDomain) &&
                Uri.TryCreate(configuredDomain, UriKind.Absolute, out var configuredUri) &&
                (configuredUri.Scheme == Uri.UriSchemeHttps || configuredUri.Scheme == Uri.UriSchemeHttp) &&
                string.IsNullOrEmpty(configuredUri.UserInfo) &&
                string.IsNullOrEmpty(configuredUri.Query) &&
                string.IsNullOrEmpty(configuredUri.Fragment))
            {
                return configuredUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
            }

            var scheme = Request.IsHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            return $"{scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
        }

        private bool HasValidOnboardingSignatureIntegrity(OnboardingRequest request)
        {
            var entityId = request.Id.ToString();
            var requiresRequester = request.Status != RequestStatus.Draft && request.Status != RequestStatus.Rejected;
            var requiresIt = request.Status is RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresEmployee = request.Status is RequestStatus.Completed or RequestStatus.Approved;

            return SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                       request.RequesterSignerName, request.RequesterSignedAt, request.RequesterSignature,
                       request.RequesterSignatureHmac, isRequired: requiresRequester) &&
                   SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                       request.ITAdminSignerName, request.ITAdminSignedAt, request.ITAdminSignature,
                       request.ITAdminSignatureHmac, isRequired: requiresIt) &&
                   SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                       request.EmployeeSignerName, request.EmployeeSignedAt, request.EmployeeSignature,
                       request.EmployeeSignatureHmac, "Employee", isRequired: requiresEmployee);
        }

        private bool HasValidOffboardingSignatureIntegrity(OffboardingRequest request)
        {
            var entityId = request.Id.ToString();
            var requiresHr = request.Status is RequestStatus.PendingApproval or RequestStatus.PendingHODApproval or RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresHod = request.Status is RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresIt = request.Status is RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresEmployee = request.Status is RequestStatus.Completed or RequestStatus.Approved;

            return SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                       request.HRSignerName, request.HRSignedAt, request.HRSignature,
                       request.HRSignatureHmac, isRequired: requiresHr) &&
                   SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                       request.HODSignerName, request.HODSignedAt, request.HODSignature,
                       request.HODSignatureHmac, isRequired: requiresHod) &&
                   SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                       request.ITAdminSignerName, request.ITAdminSignedAt, request.ITAdminSignature,
                       request.ITAdminSignatureHmac, isRequired: requiresIt) &&
                   SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                       request.EmployeeSignerName, request.EmployeeSignedAt, request.EmployeeSignature,
                       request.EmployeeSignatureHmac, "Employee", isRequired: requiresEmployee);
        }

        /// <summary>
        /// Runs the IsAuthorizedForBranch operation.
        /// </summary>
        private bool IsAuthorizedForBranch(string? requestBranch, string? createdBy)
        {
            if (!CompanyScopeHelper.CanAccessCompany(User, requestBranch)) return false;
            if (User.HasClaim("UserTitle", "IT") || User.HasClaim("UserTitle", "HR")) return true;

            return string.Equals(createdBy, User.Identity?.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Runs the IsAuthorizedAsHOD operation.
        /// </summary>
        private async Task<bool> IsAuthorizedAsHOD(string? requestBranch, string? hodEmail, string? assignedHodId = null)
        {
            if (!CompanyScopeHelper.CanAccessCompany(User, requestBranch)) return false;
            if (User.HasClaim("UserTitle", "IT") || User.HasClaim("UserTitle", "HR")) return true;

            var username = User.Identity?.Name ?? "";
            var userId = _userManager.GetUserId(User);
            var hodEmails = await IT_Form.Helpers.UserHelper.GetHODEmailsAsync(_context, username);

            return (assignedHodId != null && assignedHodId == userId) ||
                   IT_Form.Helpers.UserHelper.EmailsMatch(hodEmails, hodEmail);
        }

        /// <summary>
        /// Runs the ComposeEmail operation.
        /// </summary>
        public async Task<IActionResult> ComposeEmail(int id)
        {
            var request = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();
            if (!IsAuthorizedForBranch(request.Branch, request.CreatedByUserId) && !await IsAuthorizedAsHOD(request.Branch, request.HODEmail)) return Forbid();

            string nextSignerRole = "";
            string nextActionRequired = "";
            string toEmail = "itdept@ytcprecast.com";
            string actionUrl = $"{GetAppDomain()}/Onboarding/Details/{request.Id}";
            string actionText = "Review & Sign-Off";
            string subject;
            string introText;

            if (request.Status == IT_Form.Models.Enums.RequestStatus.PendingEmployeeSign)
            {
                toEmail = request.EmailAddress ?? string.Empty;
                nextSignerRole = request.FullName ?? "Employee";
                nextActionRequired = "review this onboarding setup and complete your acknowledgment signature";
                actionUrl = $"{GetAppDomain()}/Signature/Acknowledge?token={request.AccessToken}&type=Onboarding";
                actionText = "Review & Sign";
                subject = $"ACTION REQUIRED: Employee Acknowledgement - {request.FullName}";
                introText = $"Dear {HtmlEncoder.Default.Encode(nextSignerRole)},<br/><br/>Your onboarding setup has been completed by IT. Please review the details and sign your acknowledgment using the link below.";
            }
            else if (string.IsNullOrEmpty(request.ITAdminSignature))
            {
                nextSignerRole = "IT Department";
                nextActionRequired = "provision the requested IT assets and provide your final signature";
                subject = $"ACTION REQUIRED: New Staff Onboarding Sign-Off - {request.FullName}";
                introText = $"Dear {HtmlEncoder.Default.Encode(nextSignerRole)},<br/><br/>Please be advised that a new Staff Onboarding Request has been submitted and requires your group or department sign-off.";
            }
            else
            {
                nextSignerRole = "IT Department";
                nextActionRequired = "review the completed onboarding request";
                subject = $"ACTION REQUIRED: New Staff Onboarding Sign-Off - {request.FullName}";
                introText = $"Dear {HtmlEncoder.Default.Encode(nextSignerRole)},<br/><br/>Please be advised that a new Staff Onboarding Request has been submitted and requires your group or department sign-off.";
            }

            var detailsTable = $@"
                <tr>
                    <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee Name</td>
                    <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HtmlEncoder.Default.Encode(request.FullName ?? "")}</td>
                </tr>
                <tr style='background-color: #F8FAFC;'>
                    <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Department</td>
                    <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HtmlEncoder.Default.Encode(request.Department ?? "")}</td>
                </tr>
                <tr>
                    <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Required Action</td>
                    <td style='padding: 14px 15px; font-size: 14px; color: #1A52A3; font-weight: 700;'>{nextActionRequired}</td>
                </tr>";

            var sender = await GetSenderInfoAsync();
            string body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                "Staff Onboarding Notification",
                introText,
                detailsTable,
                actionUrl,
                actionText,
                sender.Name, sender.Title, sender.Image
            );

            var vm = new EmailConfirmationViewModel
            {
                To = string.IsNullOrWhiteSpace(toEmail) ? new List<string>() : toEmail.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList(),
                Subject = subject,
                BodyHtml = body,
                ReturnUrl = Url.Action("Details", "Onboarding", new { id = request.Id }) ?? "/",
                RequestId = request.Id,
                EmailType = "Onboarding",
                HasAttachment = false,
                SignatureImage = sender.Image
            };
            return View("ConfirmEmail", vm);
        }

        /// <summary>
        /// Runs the ComposeOffboardingEmail operation.
        /// </summary>
        public async Task<IActionResult> ComposeOffboardingEmail(int id)
        {
            var request = await _context.OffboardingRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();
            if (!IsAuthorizedForBranch(request.Branch, request.CreatedByUserId) && !await IsAuthorizedAsHOD(request.Branch, request.HODEmail, request.AssignedHodId)) return Forbid();

            string nextSignerRole = "";
            string nextActionRequired = "";
            string toEmail = "";
            string actionUrl = $"{GetAppDomain()}/Offboarding/Details/{request.Id}";
            string actionText = "Review & Sign-Off";
            string subject;
            string introText;

            if (request.Status == IT_Form.Models.Enums.RequestStatus.PendingEmployeeSign)
            {
                toEmail = request.EmailAddress ?? string.Empty;
                nextSignerRole = request.FullName ?? "Employee";
                nextActionRequired = "review this offboarding summary and complete your acknowledgment signature";
                actionUrl = $"{GetAppDomain()}/Signature/Acknowledge?token={request.AccessToken}&type=Offboarding";
                actionText = "Review & Sign";
                subject = $"ACTION REQUIRED: Employee Acknowledgement - {request.FullName}";
                introText = $"Dear {HtmlEncoder.Default.Encode(nextSignerRole)},<br/><br/>Your offboarding process has reached the final stage. Please review and sign your acknowledgment using the link below.";
            }
            else if (!string.IsNullOrEmpty(request.HRSignature) && string.IsNullOrEmpty(request.HODSignature))
            {
                toEmail = request.HODEmail ?? "";
                nextSignerRole = "Head of Department";
                nextActionRequired = "verify asset return and sign-off";
                subject = $"ACTION REQUIRED: Offboarding Sign-Off - {request.FullName}";
                introText = $"Dear {HtmlEncoder.Default.Encode(nextSignerRole)},<br/><br/>An Offboarding Request for {HtmlEncoder.Default.Encode(request.FullName ?? "")} is currently pending your action.";
            }
            else
            {
                toEmail = "itdept@ytcprecast.com";
                nextSignerRole = "IT Department";
                nextActionRequired = "revoke digital access and final sign-off";
                subject = $"ACTION REQUIRED: Offboarding Sign-Off - {request.FullName}";
                introText = $"Dear {HtmlEncoder.Default.Encode(nextSignerRole)},<br/><br/>An Offboarding Request for {HtmlEncoder.Default.Encode(request.FullName ?? "")} is currently pending your action.";
            }

            var detailsTable = $@"
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee Name</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HtmlEncoder.Default.Encode(request.FullName ?? "")}</td>
                </tr>
                <tr style='background-color: #FBFDFF;'>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Last Day of Work</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{request.LastDayOfWork:dd/MM/yyyy}</td>
                </tr>
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Required Action</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1A52A3; font-weight: 700;'>{HtmlEncoder.Default.Encode(nextActionRequired)}</td>
                </tr>";

            var sender = await GetSenderInfoAsync();
            string body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                "Staff Offboarding Notification",
                introText,
                detailsTable,
                actionUrl,
                actionText,
                sender.Name, sender.Title, sender.Image
            );

            var vm = new EmailConfirmationViewModel
            {
                To = string.IsNullOrWhiteSpace(toEmail) ? new List<string>() : toEmail.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList(),
                Subject = subject,
                BodyHtml = body,
                ReturnUrl = Url.Action("Details", "Offboarding", new { id = request.Id }) ?? "/",
                RequestId = request.Id,
                EmailType = "Offboarding",
                HasAttachment = false,
                SignatureImage = sender.Image
            };
            return View("ConfirmEmail", vm);
        }

        /// <summary>
        /// Runs the ComposeRejectionEmail operation.
        /// </summary>
        public async Task<IActionResult> ComposeRejectionEmail(int id, string type)
        {
            if (!string.Equals(type, "Onboarding", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(type, "Offboarding", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            string fullName = "";
            string reason = "";
            string toEmail = "";

            if (type == "Onboarding")
            {
                var req = await _context.OnboardingRequests.FindAsync(id);
                if (req == null) return NotFound();
                if (!IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) && !await IsAuthorizedAsHOD(req.Branch, req.HODEmail)) return Forbid();
                fullName = req.FullName ?? string.Empty;
                reason = req.RejectionReason ?? "No reason provided.";
                toEmail = req.CreatedByUserId ?? "hr@ytcprecast.com";
            }
            else
            {
                var req = await _context.OffboardingRequests.FindAsync(id);
                if (req == null) return NotFound();
                if (!IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) && !await IsAuthorizedAsHOD(req.Branch, req.HODEmail, req.AssignedHodId)) return Forbid();
                fullName = req.FullName ?? string.Empty;
                reason = req.RejectionReason ?? "No reason provided.";
                toEmail = req.CreatedByUserId ?? "hr@ytcprecast.com";
            }

            string subject = $"REJECTED: {type} Request - {fullName}";

            var detailsTable = $@"
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Request Type</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{type}</td>
                </tr>
                <tr style='background-color: #FBFDFF;'>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HtmlEncoder.Default.Encode(fullName)}</td>
                </tr>
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Rejection Reason</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #EF4444; font-weight: 700;'>{HtmlEncoder.Default.Encode(reason)}</td>
                </tr>";

            var sender = await GetSenderInfoAsync();
            string body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                "Request Rejected",
                $"Dear User,<br/><br/>Please be informed that your {HtmlEncoder.Default.Encode(type)} request has been rejected. Feedback is provided below.",
                detailsTable,
                $"{GetAppDomain()}/{type}/Details/{id}",
                "Review & Edit Request",
                sender.Name, sender.Title, sender.Image
            );

            var vm = new EmailConfirmationViewModel
            {
                To = string.IsNullOrWhiteSpace(toEmail) ? new List<string>() : toEmail.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList(),
                Subject = subject,
                BodyHtml = body,
                ReturnUrl = Url.Action("Details", type, new { id }) ?? "/",
                RequestId = id,
                EmailType = $"Rejection_{type}",
                HasAttachment = false,
                SignatureImage = sender.Image
            };
            return View("ConfirmEmail", vm);
        }



        /// <summary>
        /// Runs the ComposeEmployeeSignatureEmail operation.
        /// </summary>
        public async Task<IActionResult> ComposeEmployeeSignatureEmail(int id, string type)
        {
            if (!string.Equals(type, "Onboarding", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(type, "Offboarding", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            // Any user carrying the IT title must be blocked from employee-link generation.
            if (User.HasClaim("UserTitle", "IT") || !User.HasClaim("UserTitle", "HR"))
            {
                return Forbid();
            }

            string fullName = "";
            string toEmail = "";
            Guid token = Guid.Empty;
            byte[]? pdfBytes = null;

            if (type == "Onboarding")
            {
                var req = await _context.OnboardingRequests
                    .AsSplitQuery()
                    .IncludeOnboardingPdfData()
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (req == null) return NotFound();
                if (!IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) && !await IsAuthorizedAsHOD(req.Branch, req.HODEmail)) return Forbid();
                fullName = req.FullName ?? "";
                toEmail = req.EmailAddress ?? "";
                token = req.AccessToken;
                if (!HasValidOnboardingSignatureIntegrity(req)) return BadRequest("Signature integrity validation failed.");
                pdfBytes = _pdfService.GeneratePdf(req);
            }
            else
            {
                var req = await _context.OffboardingRequests
                    .AsSplitQuery()
                    .IncludeOffboardingPdfData()
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (req == null) return NotFound();
                if (!IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) && !await IsAuthorizedAsHOD(req.Branch, req.HODEmail, req.AssignedHodId)) return Forbid();
                fullName = req.FullName ?? "";
                toEmail = req.EmailAddress ?? "";
                token = req.AccessToken;
                if (!HasValidOffboardingSignatureIntegrity(req)) return BadRequest("Signature integrity validation failed.");
                pdfBytes = _pdfService.GenerateOffboardingPdf(req);
            }

            string subject = $"Action Required: Acknowledge {type} Details - {fullName}";
            string signatureLink = $"{GetAppDomain()}/Signature/Acknowledge?token={token}&type={type}";

            var detailsTable = $@"
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee Name</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HtmlEncoder.Default.Encode(fullName)}</td>
                </tr>
                <tr style='background-color: #FBFDFF;'>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Request Type</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{type}</td>
                </tr>
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Urgency</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #10B981; font-weight: 700;'>High Priority</td>
                </tr>";

            var sender = await GetSenderInfoAsync();
            string body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                "Personal Acknowledgement Required",
                $"Dear {HtmlEncoder.Default.Encode(fullName)},<br/><br/>Your IT {HtmlEncoder.Default.Encode(type)} process has been completed. Please review the attached PDF and provide your digital signature using the link below.",
                detailsTable,
                signatureLink,
                "Sign Your document",
                sender.Name, sender.Title, sender.Image
            );

            var vm = new EmailConfirmationViewModel
            {
                To = string.IsNullOrWhiteSpace(toEmail) ? new List<string>() : toEmail.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList(),
                Subject = subject,
                BodyHtml = body,
                ReturnUrl = Url.Action("Details", type, new { id }) ?? "/",
                RequestId = id,
                EmailType = $"SignatureAcknowledgement_{type}",
                HasAttachment = true,
                SignatureImage = sender.Image
            };
            return View("ConfirmEmail", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("workflow-email")]
        /// <summary>
        /// Runs the SendConfirmedEmail operation.
        /// </summary>
        public async Task<IActionResult> SendConfirmedEmail(EmailConfirmationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ConfirmEmail", model);
            }

            if (!model.RequestId.HasValue || string.IsNullOrWhiteSpace(model.EmailType))
            {
                return BadRequest("A valid request context is required to send workflow email.");
            }

            var emailType = model.EmailType.Trim();
            var isOnboarding = emailType is "Onboarding" or "Rejection_Onboarding" or "Nudge_Onboarding" or "SignatureAcknowledgement_Onboarding";
            var isOffboarding = emailType is "Offboarding" or "Rejection_Offboarding" or "Nudge_Offboarding" or "SignatureAcknowledgement_Offboarding";
            if (!isOnboarding && !isOffboarding)
            {
                return BadRequest("Unsupported email type.");
            }

            var isSignatureAcknowledgement = emailType.StartsWith("SignatureAcknowledgement_", StringComparison.Ordinal);
            if (isSignatureAcknowledgement &&
                (!User.HasClaim("UserTitle", "HR") || User.HasClaim("UserTitle", "IT")))
            {
                return Forbid();
            }

            byte[]? pdfBytes = null;
            string? attachmentName = null;

            if (isOnboarding)
            {
                var request = await _context.OnboardingRequests
                    .AsSplitQuery()
                    .IncludeOnboardingPdfData()
                    .FirstOrDefaultAsync(r => r.Id == model.RequestId.Value);
                if (request == null) return NotFound();
                var canSendAsManager = User.HasClaim("UserTitle", "Manager") &&
                                       IsAuthorizedForBranch(request.Branch, request.CreatedByUserId);
                if (!await IsAuthorizedAsHOD(request.Branch, request.HODEmail) && !canSendAsManager)
                {
                    return Forbid();
                }

                if (model.HasAttachment && emailType == "SignatureAcknowledgement_Onboarding")
                {
                    if (!HasValidOnboardingSignatureIntegrity(request)) return BadRequest("Signature integrity validation failed.");
                    pdfBytes = _pdfService.GeneratePdf(request);
                    var safeName = System.Text.RegularExpressions.Regex.Replace(request.FullName ?? "Employee", @"[^\p{L}\p{N}._-]+", "_").Trim('.', '_', '-');
                    if (string.IsNullOrWhiteSpace(safeName)) safeName = "Employee";
                    attachmentName = $"Onboarding_Acknowledgement_{safeName}.pdf";
                }
            }
            else
            {
                var request = await _context.OffboardingRequests
                    .AsSplitQuery()
                    .IncludeOffboardingPdfData()
                    .FirstOrDefaultAsync(r => r.Id == model.RequestId.Value);
                if (request == null) return NotFound();
                var canSendAsManager = User.HasClaim("UserTitle", "Manager") &&
                                       IsAuthorizedForBranch(request.Branch, request.CreatedByUserId);
                if (!await IsAuthorizedAsHOD(request.Branch, request.HODEmail, request.AssignedHodId) && !canSendAsManager)
                {
                    return Forbid();
                }

                if (model.HasAttachment && emailType == "SignatureAcknowledgement_Offboarding")
                {
                    if (!HasValidOffboardingSignatureIntegrity(request)) return BadRequest("Signature integrity validation failed.");
                    pdfBytes = _pdfService.GenerateOffboardingPdf(request);
                    var safeName = System.Text.RegularExpressions.Regex.Replace(request.FullName ?? "Employee", @"[^\p{L}\p{N}._-]+", "_").Trim('.', '_', '-');
                    if (string.IsNullOrWhiteSpace(safeName)) safeName = "Employee";
                    attachmentName = $"Offboarding_Acknowledgement_{safeName}.pdf";
                }
            }

            try
            {
                string toStr = string.Join(";", model.To);
                string ccStr = model.Cc != null ? string.Join(";", model.Cc) : "";
                _outlookService.DisplayEmail(toStr, model.Subject, model.BodyHtml, pdfBytes, attachmentName, ccStr, model.SignatureImage);
                TempData["SuccessMessage"] = "Email sent successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmed email.");
                TempData["ErrorMessage"] = "Failed to send email. Please check configuration.";
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        /// <summary>
        /// Runs the PreviewAttachment operation.
        /// </summary>
        public async Task<IActionResult> PreviewAttachment(int id, string type)
        {
            byte[]? pdfBytes = null;
            if (type.StartsWith("SignatureAcknowledgement_Onboarding"))
            {
                var req = await _context.OnboardingRequests
                    .AsSplitQuery()
                    .IncludeOnboardingPdfData()
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (req != null && (IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) || await IsAuthorizedAsHOD(req.Branch, req.HODEmail)) && HasValidOnboardingSignatureIntegrity(req)) pdfBytes = _pdfService.GeneratePdf(req);
            }
            else if (type.StartsWith("SignatureAcknowledgement_Offboarding"))
            {
                var req = await _context.OffboardingRequests
                    .AsSplitQuery()
                    .IncludeOffboardingPdfData()
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (req != null && (IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) || await IsAuthorizedAsHOD(req.Branch, req.HODEmail, req.AssignedHodId)) && HasValidOffboardingSignatureIntegrity(req)) pdfBytes = _pdfService.GenerateOffboardingPdf(req);
            }

            if (pdfBytes != null)
            {
                return File(pdfBytes, "application/pdf");
            }
            return NotFound("Attachment preview not available.");
        }

        /// <summary>
        /// Runs the ComposeNudgeEmail operation.
        /// </summary>
        public async Task<IActionResult> ComposeNudgeEmail(int id, string type)
        {
            if (!string.Equals(type, "Onboarding", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(type, "Offboarding", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            string fullName = "";
            string toEmail = "";
            string returnUrl = "";
            string detailsTable = "";
            string detailsUrl = "";

            if (type == "Onboarding")
            {
                var req = await _context.OnboardingRequests.FindAsync(id);
                if (req == null) return NotFound();
                if (!IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) && !await IsAuthorizedAsHOD(req.Branch, req.HODEmail)) return Forbid();
                fullName = req.FullName ?? "";
                toEmail = req.HODEmail ?? "";
                returnUrl = Url.Action("Details", "Onboarding", new { id }) ?? "/";
                detailsUrl = $"{GetAppDomain()}/Onboarding/Details/{id}";

                detailsTable = $@"
                    <tr>
                        <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee Name</td>
                        <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HtmlEncoder.Default.Encode(fullName)}</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Pending Action</td>
                        <td style='padding: 12px 15px; font-size: 14px; color: #B45309; font-weight: 700;'>HOD Approval Required</td>
                    </tr>";
            }
            else if (type == "Offboarding")
            {
                var req = await _context.OffboardingRequests.FindAsync(id);
                if (req == null) return NotFound();
                if (!IsAuthorizedForBranch(req.Branch, req.CreatedByUserId) && !await IsAuthorizedAsHOD(req.Branch, req.HODEmail, req.AssignedHodId)) return Forbid();
                fullName = req.FullName ?? "";
                toEmail = req.HODEmail ?? "";
                returnUrl = Url.Action("Details", "Offboarding", new { id }) ?? "/";
                detailsUrl = $"{GetAppDomain()}/Offboarding/Details/{id}";

                detailsTable = $@"
                    <tr>
                        <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee Name</td>
                        <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HtmlEncoder.Default.Encode(fullName)}</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Pending Action</td>
                        <td style='padding: 12px 15px; font-size: 14px; color: #B45309; font-weight: 700;'>Manager Checklist Required</td>
                    </tr>";
            }

            string subject = $"FOLLOW-UP: Signature Required for {type} Request - {fullName}";

            var sender = await GetSenderInfoAsync();
            string body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                "Manager Approval Reminder",
                $"Dear Manager,<br/><br/>This is a friendly reminder regarding the pending {HtmlEncoder.Default.Encode(type)} request for <strong>{HtmlEncoder.Default.Encode(fullName)}</strong>. Your signature is required to proceed with the IT provisioning process.",
                detailsTable,
                detailsUrl,
                "Review & Sign-Off",
                sender.Name, sender.Title, sender.Image
            );

            var vm = new EmailConfirmationViewModel
            {
                To = string.IsNullOrWhiteSpace(toEmail) ? new List<string>() : toEmail.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList(),
                Subject = subject,
                BodyHtml = body,
                ReturnUrl = returnUrl,
                RequestId = id,
                EmailType = $"Nudge_{type}",
                HasAttachment = false,
                SignatureImage = sender.Image
            };
            return View("ConfirmEmail", vm);
        }
    }
}
