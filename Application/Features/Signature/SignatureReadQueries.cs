using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Services;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Signature;

public sealed record GetSignatureAcknowledgePageQuery(Guid Token, string Type) : IRequest<GetSignatureAcknowledgePageResult>;
public sealed record GetSignaturePdfQuery(Guid Token, string Type) : IRequest<GetSignaturePdfResult>;

public enum GetSignatureAcknowledgePageStatus
{
    Ready,
    Invalid,
    NotFound,
    IntegrityFailed
}

public sealed record GetSignatureAcknowledgePageResult(
    GetSignatureAcknowledgePageStatus Status,
    SignatureAcknowledgePageViewModel? ViewModel = null,
    string? Message = null);

public enum GetSignaturePdfStatus
{
    Ready,
    Invalid,
    NotFound,
    Expired,
    IntegrityFailed
}

public sealed record SignaturePdfFilePayload(
    byte[] Content,
    string ContentType,
    string FileDownloadName);

public sealed record GetSignaturePdfResult(
    GetSignaturePdfStatus Status,
    SignaturePdfFilePayload? File = null,
    string? Message = null);

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
                    item.AccessToken,
                    item.FullName,
                    item.Department,
                    Company = item.Branch,
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

            if (record.EmployeeSignedAt.HasValue && !string.IsNullOrEmpty(record.EmployeeSignatureHmac) &&
                !_integrityService.Verify(
                    "OnboardingRequest",
                    record.Id.ToString(),
                    record.EmployeeSignerName ?? "Employee",
                    record.EmployeeSignedAt.Value,
                    record.EmployeeSignature ?? string.Empty,
                    record.EmployeeSignatureHmac))
            {
                return new(
                    GetSignatureAcknowledgePageStatus.IntegrityFailed,
                    Message: "Digital signature integrity check failed. The document may have been tampered with.");
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
                    item.AccessToken,
                    item.FullName,
                    item.Department,
                    Company = item.Branch,
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

            if (record.EmployeeSignedAt.HasValue && !string.IsNullOrEmpty(record.EmployeeSignatureHmac) &&
                !_integrityService.Verify(
                    "OffboardingRequest",
                    record.Id.ToString(),
                    record.EmployeeSignerName ?? "Employee",
                    record.EmployeeSignedAt.Value,
                    record.EmployeeSignature ?? string.Empty,
                    record.EmployeeSignatureHmac))
            {
                return new(
                    GetSignatureAcknowledgePageStatus.IntegrityFailed,
                    Message: "Digital signature integrity check failed. The document may have been tampered with.");
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

            if (onboardingRequest.EmployeeSignedAt.HasValue && !string.IsNullOrEmpty(onboardingRequest.EmployeeSignatureHmac) &&
                !_integrityService.Verify("OnboardingRequest", onboardingRequest.Id.ToString(), onboardingRequest.EmployeeSignerName ?? "Employee", onboardingRequest.EmployeeSignedAt.Value, onboardingRequest.EmployeeSignature ?? string.Empty, onboardingRequest.EmployeeSignatureHmac))
            {
                return new(GetSignaturePdfStatus.IntegrityFailed, Message: "Signature Integrity Violation (Employee)");
            }

            if (onboardingRequest.ITAdminSignedAt.HasValue && !string.IsNullOrEmpty(onboardingRequest.ITAdminSignatureHmac) &&
                !_integrityService.Verify("OnboardingRequest", onboardingRequest.Id.ToString(), onboardingRequest.ITAdminSignerName ?? "Unknown", onboardingRequest.ITAdminSignedAt.Value, onboardingRequest.ITAdminSignature ?? string.Empty, onboardingRequest.ITAdminSignatureHmac))
            {
                return new(GetSignaturePdfStatus.IntegrityFailed, Message: "Signature Integrity Violation (IT Admin)");
            }

            var pdfBytes = _pdfService.GeneratePdf(onboardingRequest);
            return new(
                GetSignaturePdfStatus.Ready,
                new SignaturePdfFilePayload(
                    pdfBytes,
                    "application/pdf",
                    $"Onboarding_{onboardingRequest.FullName?.Replace(" ", "_")}.pdf"));
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

            if (offboardingRequest.EmployeeSignedAt.HasValue && !string.IsNullOrEmpty(offboardingRequest.EmployeeSignatureHmac) &&
                !_integrityService.Verify("OffboardingRequest", offboardingRequest.Id.ToString(), offboardingRequest.EmployeeSignerName ?? "Employee", offboardingRequest.EmployeeSignedAt.Value, offboardingRequest.EmployeeSignature ?? string.Empty, offboardingRequest.EmployeeSignatureHmac))
            {
                return new(GetSignaturePdfStatus.IntegrityFailed, Message: "Signature Integrity Violation (Employee)");
            }

            var pdfBytes = _pdfService.GenerateOffboardingPdf(offboardingRequest);
            return new(
                GetSignaturePdfStatus.Ready,
                new SignaturePdfFilePayload(
                    pdfBytes,
                    "application/pdf",
                    $"Offboarding_{offboardingRequest.FullName?.Replace(" ", "_")}.pdf"));
        }

        return new(GetSignaturePdfStatus.Invalid, Message: "Unknown request type.");
    }
}
