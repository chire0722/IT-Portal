// Project comment: AcknowledgeEmployeeSignatureCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.Application.Features.Signature;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Signature.AcknowledgeEmployeeSignature
{
    /// <summary>
    /// Handles the AcknowledgeEmployeeSignatureCommand command workflow.
    /// </summary>
    public sealed class AcknowledgeEmployeeSignatureCommandHandler
        : IRequestHandler<AcknowledgeEmployeeSignatureCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AcknowledgeEmployeeSignatureCommandHandler> _logger;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly IEmailBackgroundQueue _emailBackgroundQueue;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AcknowledgeEmployeeSignatureCommandHandler(
            ApplicationDbContext context,
            ILogger<AcknowledgeEmployeeSignatureCommandHandler> logger,
            ISignatureIntegrityService integrityService,
            IEmailBackgroundQueue emailBackgroundQueue,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _integrityService = integrityService;
            _emailBackgroundQueue = emailBackgroundQueue;
            _userManager = userManager;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(
            AcknowledgeEmployeeSignatureCommand request,
            CancellationToken cancellationToken)
        {
            // Enforce consent at the handler boundary as well as in FluentValidation,
            // so direct MediatR handler invocation cannot record consent that was not given.
            if (!request.PdpaConsent)
            {
                return ProcessResult<object>.Failure("Please confirm the PDPA privacy notice before submitting.");
            }

            if (string.Equals(request.Type, "Onboarding", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleOnboardingAsync(request, cancellationToken);
            }

            if (string.Equals(request.Type, "Offboarding", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleOffboardingAsync(request, cancellationToken);
            }

            return ProcessResult<object>.Failure("Unknown request type.");
        }

        /// <summary>
        /// Runs the HandleOnboardingAsync operation.
        /// </summary>
        private async Task<ProcessResult<object>> HandleOnboardingAsync(
            AcknowledgeEmployeeSignatureCommand request,
            CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .FirstOrDefaultAsync(item => item.AccessToken == request.Token, cancellationToken);
            if (onboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (SignatureLinkExpiry.IsExpired(
                onboardingRequest.CreatedAt,
                onboardingRequest.UpdatedAt,
                onboardingRequest.CompletedAt,
                SignatureLinkExpiry.EmployeeSignatureLifetime,
                DateTime.UtcNow))
            {
                return ProcessResult<object>.Failure("This link has expired (older than 7 days).");
            }

            if (onboardingRequest.Status != RequestStatus.PendingEmployeeSign)
            {
                return ProcessResult<object>.Failure("This request is not currently awaiting employee signature.");
            }

            if (!string.IsNullOrEmpty(onboardingRequest.EmployeeSignature))
            {
                return ProcessResult<object>.Failure("This document has already been signed.");
            }

            var signedAt = DateTime.UtcNow;
            onboardingRequest.EmployeeSignature = request.SignatureData;
            onboardingRequest.EmployeeSignedAt = signedAt;
            onboardingRequest.EmployeeSignerName = onboardingRequest.FullName ?? "Employee";
            onboardingRequest.EmployeeSignatureHmac = _integrityService.ComputeHmac(
                "OnboardingRequest",
                onboardingRequest.Id.ToString(),
                onboardingRequest.EmployeeSignerName,
                signedAt,
                request.SignatureData);
            onboardingRequest.IsPdpaConsented = true;
            onboardingRequest.PdpaConsentedAt = signedAt;
            onboardingRequest.Status = RequestStatus.Completed;
            onboardingRequest.CompletedAt = signedAt;
            onboardingRequest.UpdatedAt = signedAt;

            await QueueCompletionNotificationAsync(
                onboardingRequest.FullName ?? "Employee",
                "Onboarding",
                onboardingRequest.CreatedByUserId,
                onboardingRequest.Id,
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return ProcessResult<object>.Success(message: "Acknowledgement completed successfully.");
        }

        /// <summary>
        /// Runs the HandleOffboardingAsync operation.
        /// </summary>
        private async Task<ProcessResult<object>> HandleOffboardingAsync(
            AcknowledgeEmployeeSignatureCommand request,
            CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests
                .FirstOrDefaultAsync(item => item.AccessToken == request.Token, cancellationToken);
            if (offboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (SignatureLinkExpiry.IsExpired(
                offboardingRequest.CreatedAt,
                offboardingRequest.UpdatedAt,
                offboardingRequest.CompletedAt,
                SignatureLinkExpiry.EmployeeSignatureLifetime,
                DateTime.UtcNow))
            {
                return ProcessResult<object>.Failure("This link has expired (older than 7 days).");
            }

            if (offboardingRequest.Status != RequestStatus.PendingEmployeeSign)
            {
                return ProcessResult<object>.Failure("This request is not currently awaiting employee signature.");
            }

            if (!string.IsNullOrEmpty(offboardingRequest.EmployeeSignature))
            {
                return ProcessResult<object>.Failure("This document has already been signed.");
            }

            var signedAt = DateTime.UtcNow;
            offboardingRequest.EmployeeSignature = request.SignatureData;
            offboardingRequest.EmployeeSignedAt = signedAt;
            offboardingRequest.EmployeeSignerName = offboardingRequest.FullName ?? "Employee";
            offboardingRequest.EmployeeSignatureHmac = _integrityService.ComputeHmac(
                "OffboardingRequest",
                offboardingRequest.Id.ToString(),
                offboardingRequest.EmployeeSignerName,
                signedAt,
                request.SignatureData);
            offboardingRequest.IsPdpaConsented = true;
            offboardingRequest.PdpaConsentedAt = signedAt;
            offboardingRequest.CompletedAt = signedAt;
            offboardingRequest.Status = RequestStatus.Completed;
            offboardingRequest.UpdatedAt = signedAt;

            if (offboardingRequest.OnboardingRequestId.HasValue)
            {
                var keys = await _context.SoftwareKeys
                    .Where(key => key.OnboardingRequestId == offboardingRequest.OnboardingRequestId.Value && key.IsAssigned)
                    .ToListAsync(cancellationToken);

                foreach (var key in keys)
                {
                    key.IsAssigned = false;
                    key.OnboardingRequestId = null;
                    key.AssignedToUserId = null;
                    key.MachineName = null;
                    key.AssignmentDate = null;
                }
            }

            await QueueCompletionNotificationAsync(
                offboardingRequest.FullName ?? "Employee",
                "Offboarding",
                offboardingRequest.CreatedByUserId,
                offboardingRequest.Id,
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return ProcessResult<object>.Success(message: "Acknowledgement completed successfully.");
        }

        /// <summary>
        /// Runs the QueueCompletionNotificationAsync operation.
        /// </summary>
        private async Task QueueCompletionNotificationAsync(
            string employeeName,
            string type,
            string? creatorUserId,
            int requestId,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "SendCompletionNotification: Starting for {Type} #{Id}, CreatorUserName={UserName}",
                    type,
                    requestId,
                    creatorUserId ?? "(null)");

                if (string.IsNullOrEmpty(creatorUserId))
                {
                    _logger.LogWarning(
                        "SendCompletionNotification: CreatorUserName is null/empty for {Type} #{Id}. Cannot send.",
                        type,
                        requestId);
                    return;
                }

                var creator = await _userManager.FindByNameAsync(creatorUserId)
                    ?? await _userManager.FindByIdAsync(creatorUserId)
                    ?? await _userManager.FindByEmailAsync(creatorUserId);
                if (creator == null)
                {
                    _logger.LogWarning(
                        "SendCompletionNotification: Creator user not found for UserName={UserName}.",
                        creatorUserId);
                    return;
                }

                if (string.IsNullOrEmpty(creator.Email))
                {
                    _logger.LogWarning(
                        "SendCompletionNotification: Creator {UserName} has no email address.",
                        creator.UserName);
                    return;
                }

                var request = _httpContextAccessor.HttpContext?.Request;
                var appDomainSetting = _configuration["AppDomain"] ?? string.Empty;
                var appDomain = appDomainSetting.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                    ?? (request != null ? $"{request.Scheme}://{request.Host}" : string.Empty);
                var detailsUrl = $"{appDomain}/{type}/Details/{requestId}";

                var subject = $"COMPLETED: {type} Sign-Off - {employeeName}";
                var safeCreatorName = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(creator.FullName ?? creator.UserName ?? "User");
                var safeEmployeeName = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(employeeName);
                var greeting = $"Dear {safeCreatorName},";
                var paramsRows = $@"
                    <tr>
                        <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee Name</td>
                        <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(employeeName)}</td>
                    </tr>
                    <tr style='background-color: #F8FAFC;'>
                        <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Completion Date</td>
                        <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{DateTime.Now:dd MMM yyyy, hh:mm tt}</td>
                    </tr>
                    <tr>
                        <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; font-weight: 600;'>Status</td>
                        <td style='padding: 14px 15px; font-size: 14px; color: #10B981; font-weight: 700;'>COMPLETED</td>
                    </tr>";

                var senderName = creator.FullName;
                var senderTitle = creator.JobTitle;
                var senderSignature = creator.EmailSignatureImage;
                var htmlBody = Helpers.EmailTemplateHelper.GetProfessionalHtmlTemplate(
                    $"{type} Process Completed",
                    $"{greeting}<br/><br/>The digital sign-off for <strong>{safeEmployeeName}</strong> has been successfully completed by the employee. The request is now archived as Completed.",
                    paramsRows,
                    detailsUrl,
                    "View Completed Request",
                    senderName,
                    senderTitle,
                    senderSignature);

                await _emailBackgroundQueue.QueueEmailAsync(
                    new RenderedEmailMessage(
                        creator.Email,
                        subject,
                        htmlBody,
                        SignatureBase64: senderSignature),
                    cancellationToken);
                _logger.LogInformation(
                    "Completion notification queued successfully for {Type} #{Id}.",
                    type,
                    requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue completion notification for {Type} {Id}", type, requestId);
            }
        }
    }
}
