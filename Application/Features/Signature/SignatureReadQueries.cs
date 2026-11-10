// Project comment: SignatureReadQueries.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Signature;

/// <summary>
/// Carries data needed to execute the GetSignatureAcknowledgePage query.
/// </summary>
public sealed record GetSignatureAcknowledgePageQuery(Guid Token, string Type) : IRequest<GetSignatureAcknowledgePageResult>;
/// <summary>
/// Carries data needed to execute the GetSignaturePdf query.
/// </summary>
public sealed record GetSignaturePdfQuery(Guid Token, string Type) : IRequest<GetSignaturePdfResult>;

/// <summary>
/// Lists the supported GetSignatureAcknowledgePageStatus values used by the application.
/// </summary>
public enum GetSignatureAcknowledgePageStatus
{
    Ready,
    Invalid,
    NotFound,
    IntegrityFailed
}

/// <summary>
/// Represents the result returned by the GetSignatureAcknowledgePage workflow.
/// </summary>
public sealed record GetSignatureAcknowledgePageResult(
    GetSignatureAcknowledgePageStatus Status,
    SignatureAcknowledgePageViewModel? ViewModel = null,
    string? Message = null);

/// <summary>
/// Lists the supported GetSignaturePdfStatus values used by the application.
/// </summary>
public enum GetSignaturePdfStatus
{
    Ready,
    Invalid,
    NotFound,
    Expired,
    IntegrityFailed
}

/// <summary>
/// Defines the SignaturePdfFilePayload record used by the IT Form application.
/// </summary>
public sealed record SignaturePdfFilePayload(
    byte[] Content,
    string ContentType,
    string FileDownloadName);

/// <summary>
/// Represents the result returned by the GetSignaturePdf workflow.
/// </summary>
public sealed record GetSignaturePdfResult(
    GetSignaturePdfStatus Status,
    SignaturePdfFilePayload? File = null,
    string? Message = null);

/// <summary>
/// Handles the GetSignatureAcknowledgePageQuery query workflow.
/// </summary>
public sealed class GetSignatureAcknowledgePageQueryHandler
    : IRequestHandler<GetSignatureAcknowledgePageQuery, GetSignatureAcknowledgePageResult>
{
    private readonly ApplicationDbContext _context;
    private readonly ISignatureIntegrityService _integrityService;

    public GetSignatureAcknowledgePageQueryHandler(
        ApplicationDbContext context,
        ISignatureIntegrityService integrityService)
    {
        _context = context;
        _integrityService = integrityService;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<GetSignatureAcknowledgePageResult> Handle(GetSignatureAcknowledgePageQuery request, CancellationToken cancellationToken)
    {
        if (request.Token == Guid.Empty || string.IsNullOrWhiteSpace(request.Type))
        {
            return new(GetSignatureAcknowledgePageStatus.Invalid, Message: "Invalid token or type.");
        }

        if (string.Equals(request.Type, "Onboarding", StringComparison.OrdinalIgnoreCase))
        {
            var record = await _context.OnboardingRequests
                .AsNoTracking()
                .Where(item => item.AccessToken == request.Token)
                .Select(item => new
                {
                    item.Id,
                    item.Status,
                    item.AccessToken,
                    item.FullName,
                    item.Department,
                    Company = item.Branch,
                    item.RequesterSignature,
                    item.RequesterSignedAt,
                    item.RequesterSignatureHmac,
                    item.RequesterSignerName,
                    item.ITAdminSignature,
                    item.ITAdminSignedAt,
                    item.ITAdminSignatureHmac,
                    item.ITAdminSignerName,
                    item.EmployeeSignature,
                    item.EmployeeSignedAt,
                    item.EmployeeSignatureHmac,
                    item.EmployeeSignerName,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.CompletedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (record == null)
            {
                return new(GetSignatureAcknowledgePageStatus.NotFound, Message: "Request not found or token invalid.");
            }

            var onboardingId = record.Id.ToString();
            var requiresRequester = record.Status != RequestStatus.Draft && record.Status != RequestStatus.Rejected;
            var requiresIt = record.Status is RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresEmployee = record.Status is RequestStatus.Completed or RequestStatus.Approved;
            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", onboardingId, record.RequesterSignerName, record.RequesterSignedAt, record.RequesterSignature, record.RequesterSignatureHmac, isRequired: requiresRequester) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", onboardingId, record.ITAdminSignerName, record.ITAdminSignedAt, record.ITAdminSignature, record.ITAdminSignatureHmac, isRequired: requiresIt) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", onboardingId, record.EmployeeSignerName, record.EmployeeSignedAt, record.EmployeeSignature, record.EmployeeSignatureHmac, "Employee", isRequired: requiresEmployee))
            {
                return new(
                    GetSignatureAcknowledgePageStatus.IntegrityFailed,
                    Message: "A previous signature is missing integrity metadata or cannot be trusted. Contact HR to review the record.");
            }

            var model = new SignatureAcknowledgePageViewModel
            {
                Token = record.AccessToken,
                RequestType = "Onboarding",
                FullName = record.FullName,
                Department = record.Department,
                Company = record.Company,
                AlreadySigned = !string.IsNullOrEmpty(record.EmployeeSignature),
                Expired = SignatureLinkExpiry.IsExpired(
                    record.CreatedAt,
                    record.UpdatedAt,
                    record.CompletedAt,
                    SignatureLinkExpiry.EmployeeSignatureLifetime,
                    DateTime.UtcNow)
            };

            return new(GetSignatureAcknowledgePageStatus.Ready, model);
        }

        if (string.Equals(request.Type, "Offboarding", StringComparison.OrdinalIgnoreCase))
        {
            var record = await _context.OffboardingRequests
                .AsNoTracking()
                .Where(item => item.AccessToken == request.Token)
                .Select(item => new
                {
                    item.Id,
                    item.Status,
                    item.AccessToken,
                    item.FullName,
                    item.Department,
                    Company = item.Branch,
                    item.HRSignature,
                    item.HRSignedAt,
                    item.HRSignatureHmac,
                    item.HRSignerName,
                    item.HODSignature,
                    item.HODSignedAt,
                    item.HODSignatureHmac,
                    item.HODSignerName,
                    item.ITAdminSignature,
                    item.ITAdminSignedAt,
                    item.ITAdminSignatureHmac,
                    item.ITAdminSignerName,
                    item.EmployeeSignature,
                    item.EmployeeSignedAt,
                    item.EmployeeSignatureHmac,
                    item.EmployeeSignerName,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.CompletedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (record == null)
            {
                return new(GetSignatureAcknowledgePageStatus.NotFound, Message: "Request not found or token invalid.");
            }

            var offboardingId = record.Id.ToString();
            var requiresHr = record.Status is RequestStatus.PendingApproval or RequestStatus.PendingHODApproval or RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresHod = record.Status is RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresIt = record.Status is RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresEmployee = record.Status is RequestStatus.Completed or RequestStatus.Approved;
            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, record.HRSignerName, record.HRSignedAt, record.HRSignature, record.HRSignatureHmac, isRequired: requiresHr) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, record.HODSignerName, record.HODSignedAt, record.HODSignature, record.HODSignatureHmac, isRequired: requiresHod) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, record.ITAdminSignerName, record.ITAdminSignedAt, record.ITAdminSignature, record.ITAdminSignatureHmac, isRequired: requiresIt) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, record.EmployeeSignerName, record.EmployeeSignedAt, record.EmployeeSignature, record.EmployeeSignatureHmac, "Employee", isRequired: requiresEmployee))
            {
                return new(
                    GetSignatureAcknowledgePageStatus.IntegrityFailed,
                    Message: "A previous signature is missing integrity metadata or cannot be trusted. Contact HR to review the record.");
            }

            var model = new SignatureAcknowledgePageViewModel
            {
                Token = record.AccessToken,
                RequestType = "Offboarding",
                FullName = record.FullName,
                Department = record.Department,
                Company = record.Company,
                AlreadySigned = !string.IsNullOrEmpty(record.EmployeeSignature),
                Expired = SignatureLinkExpiry.IsExpired(
                    record.CreatedAt,
                    record.UpdatedAt,
                    record.CompletedAt,
                    SignatureLinkExpiry.EmployeeSignatureLifetime,
                    DateTime.UtcNow)
            };

            return new(GetSignatureAcknowledgePageStatus.Ready, model);
        }

        return new(GetSignatureAcknowledgePageStatus.Invalid, Message: "Unknown request type.");
    }
}

/// <summary>
/// Handles the GetSignaturePdfQuery query workflow.
/// </summary>
public sealed class GetSignaturePdfQueryHandler
    : IRequestHandler<GetSignaturePdfQuery, GetSignaturePdfResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfService _pdfService;
    private readonly ISignatureIntegrityService _integrityService;

    public GetSignaturePdfQueryHandler(
        ApplicationDbContext context,
        IPdfService pdfService,
        ISignatureIntegrityService integrityService)
    {
        _context = context;
        _pdfService = pdfService;
        _integrityService = integrityService;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<GetSignaturePdfResult> Handle(GetSignaturePdfQuery request, CancellationToken cancellationToken)
    {
        if (request.Token == Guid.Empty || string.IsNullOrWhiteSpace(request.Type))
        {
            return new(GetSignaturePdfStatus.Invalid, Message: "Invalid token or type.");
        }

        if (string.Equals(request.Type, "Onboarding", StringComparison.OrdinalIgnoreCase))
        {
            var onboardingRequest = await _context.OnboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOnboardingPdfData()
                .FirstOrDefaultAsync(item => item.AccessToken == request.Token, cancellationToken);

            if (onboardingRequest == null)
            {
                return new(GetSignaturePdfStatus.NotFound);
            }

            if (SignatureLinkExpiry.IsExpired(
                onboardingRequest.CreatedAt,
                onboardingRequest.UpdatedAt,
                onboardingRequest.CompletedAt,
                SignatureLinkExpiry.PdfLifetime,
                DateTime.UtcNow))
            {
                return new(
                    GetSignaturePdfStatus.Expired,
                    Message: "This access link has expired (30 day limit). Please contact HR for a new one.");
            }

            var onboardingId = onboardingRequest.Id.ToString();
            var requiresRequester = onboardingRequest.Status != RequestStatus.Draft && onboardingRequest.Status != RequestStatus.Rejected;
            var requiresIt = onboardingRequest.Status is RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresEmployee = onboardingRequest.Status is RequestStatus.Completed or RequestStatus.Approved;
            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", onboardingId, onboardingRequest.RequesterSignerName, onboardingRequest.RequesterSignedAt, onboardingRequest.RequesterSignature, onboardingRequest.RequesterSignatureHmac, isRequired: requiresRequester) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", onboardingId, onboardingRequest.ITAdminSignerName, onboardingRequest.ITAdminSignedAt, onboardingRequest.ITAdminSignature, onboardingRequest.ITAdminSignatureHmac, isRequired: requiresIt) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", onboardingId, onboardingRequest.EmployeeSignerName, onboardingRequest.EmployeeSignedAt, onboardingRequest.EmployeeSignature, onboardingRequest.EmployeeSignatureHmac, "Employee", isRequired: requiresEmployee))
            {
                return new(GetSignaturePdfStatus.IntegrityFailed, Message: "Signature integrity metadata is missing, compromised, or invalid.");
            }

            var pdfBytes = _pdfService.GeneratePdf(onboardingRequest);
            return new(
                GetSignaturePdfStatus.Ready,
                new SignaturePdfFilePayload(
                    pdfBytes,
                    "application/pdf",
                    $"Onboarding_{CreateSafeDownloadName(onboardingRequest.FullName)}.pdf"));
        }

        if (string.Equals(request.Type, "Offboarding", StringComparison.OrdinalIgnoreCase))
        {
            var offboardingRequest = await _context.OffboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOffboardingPdfData()
                .FirstOrDefaultAsync(item => item.AccessToken == request.Token, cancellationToken);

            if (offboardingRequest == null)
            {
                return new(GetSignaturePdfStatus.NotFound);
            }

            if (SignatureLinkExpiry.IsExpired(
                offboardingRequest.CreatedAt,
                offboardingRequest.UpdatedAt,
                offboardingRequest.CompletedAt,
                SignatureLinkExpiry.PdfLifetime,
                DateTime.UtcNow))
            {
                return new(GetSignaturePdfStatus.Expired, Message: "This access link has expired (30 day limit).");
            }

            var offboardingId = offboardingRequest.Id.ToString();
            var requiresHr = offboardingRequest.Status is RequestStatus.PendingApproval or RequestStatus.PendingHODApproval or RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresHod = offboardingRequest.Status is RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresIt = offboardingRequest.Status is RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresEmployee = offboardingRequest.Status is RequestStatus.Completed or RequestStatus.Approved;
            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, offboardingRequest.HRSignerName, offboardingRequest.HRSignedAt, offboardingRequest.HRSignature, offboardingRequest.HRSignatureHmac, isRequired: requiresHr) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, offboardingRequest.HODSignerName, offboardingRequest.HODSignedAt, offboardingRequest.HODSignature, offboardingRequest.HODSignatureHmac, isRequired: requiresHod) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, offboardingRequest.ITAdminSignerName, offboardingRequest.ITAdminSignedAt, offboardingRequest.ITAdminSignature, offboardingRequest.ITAdminSignatureHmac, isRequired: requiresIt) ||
                !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", offboardingId, offboardingRequest.EmployeeSignerName, offboardingRequest.EmployeeSignedAt, offboardingRequest.EmployeeSignature, offboardingRequest.EmployeeSignatureHmac, "Employee", isRequired: requiresEmployee))
            {
                return new(GetSignaturePdfStatus.IntegrityFailed, Message: "Signature integrity metadata is missing, compromised, or invalid.");
            }

            var pdfBytes = _pdfService.GenerateOffboardingPdf(offboardingRequest);
            return new(
                GetSignaturePdfStatus.Ready,
                new SignaturePdfFilePayload(
                    pdfBytes,
                    "application/pdf",
                    $"Offboarding_{CreateSafeDownloadName(offboardingRequest.FullName)}.pdf"));
        }

        return new(GetSignaturePdfStatus.Invalid, Message: "Unknown request type.");
    }

    private static string CreateSafeDownloadName(string? fullName)
    {
        var safeName = System.Text.RegularExpressions.Regex.Replace(
            fullName ?? "Employee",
            @"[^\\p{L}\\p{N}._-]+",
            "_").Trim('.', '_', '-');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "Employee";
        }

        return safeName.Length > 80 ? safeName[..80] : safeName;
    }
}
