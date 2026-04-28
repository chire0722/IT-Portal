// Project comment: OnboardingDocumentService.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Claims;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides OnboardingDocument service behavior for the application.
    /// </summary>
    public sealed class OnboardingDocumentService : IOnboardingDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ILogger<OnboardingDocumentService> _logger;

        public OnboardingDocumentService(
            ApplicationDbContext context,
            IPdfService pdfService,
            ISignatureIntegrityService integrityService,
            ILogger<OnboardingDocumentService> logger)
        {
            _context = context;
            _pdfService = pdfService;
            _integrityService = integrityService;
            _logger = logger;
        }

        /// <summary>
        /// Runs the ExportBulkPdfAsync operation.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> ExportBulkPdfAsync(int[] ids, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            if (ids == null || ids.Length == 0)
            {
                return ProcessResult<BinaryFilePayload>.Failure("No requests selected.", "Index");
            }

            var requests = await _context.OnboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOnboardingPdfData()
                .Where(r => ids.Contains(r.Id))
                .ToListAsync(cancellationToken);

            requests = requests
                .Where(request => HrRecordReadCompanyScopeSupport.CanAccessCompany(user, request.Branch))
                .ToList();

            if (!requests.Any())
            {
                return ProcessResult<BinaryFilePayload>.Failure("No valid requests found.", "Index");
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var request in requests)
                {
                    try
                    {
                        var pdfBytes = _pdfService.GeneratePdf(request);
                        var safeName = string.Join("_", (request.FullName ?? "Unknown").Split(Path.GetInvalidFileNameChars()));
                        var entry = archive.CreateEntry($"Onboarding_{safeName}_ID{request.Id}.pdf", CompressionLevel.Fastest);
                        using var entryStream = entry.Open();
                        entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate PDF for Onboarding Request {Id}", request.Id);
                    }
                }
            }

            memoryStream.Position = 0;
            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                memoryStream.ToArray(),
                "application/zip",
                $"Onboarding_PDFs_{DateTime.Now:yyyyMMdd}.zip"));
        }

        /// <summary>
        /// Runs the GeneratePdfAsync operation.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> GeneratePdfAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            var request = await _context.OnboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOnboardingPdfData()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (request == null)
            {
                return ProcessResult<BinaryFilePayload>.NotFound();
            }

            var currentUsername = user.Identity?.Name;
            var isCreator = request.CreatedByUserId == currentUsername;
            var isPrivileged = user.HasClaim("UserTitle", "IT") || user.HasClaim("UserTitle", "HR");

            if (!HrRecordReadCompanyScopeSupport.CanAccessCompany(user, request.Branch))
            {
                return ProcessResult<BinaryFilePayload>.Forbidden();
            }

            if (!isPrivileged && !isCreator)
            {
                return ProcessResult<BinaryFilePayload>.Forbidden();
            }

            if (request.RequesterSignedAt.HasValue && !string.IsNullOrEmpty(request.RequesterSignatureHmac))
            {
                if (!_integrityService.Verify("OnboardingRequest", request.Id.ToString(), request.RequesterSignerName ?? "Unknown", request.RequesterSignedAt.Value, request.RequesterSignature ?? "", request.RequesterSignatureHmac))
                {
                    return ProcessResult<BinaryFilePayload>.Failure("Digital signature integrity check failed (Requester). The document may have been tampered with.", "Details", id);
                }
            }

            if (request.ITAdminSignedAt.HasValue && !string.IsNullOrEmpty(request.ITAdminSignatureHmac))
            {
                if (!_integrityService.Verify("OnboardingRequest", request.Id.ToString(), request.ITAdminSignerName ?? "Unknown", request.ITAdminSignedAt.Value, request.ITAdminSignature ?? "", request.ITAdminSignatureHmac))
                {
                    return ProcessResult<BinaryFilePayload>.Failure("Digital signature integrity check failed (IT Admin). The document may have been tampered with.", "Details", id);
                }
            }

            if (request.EmployeeSignedAt.HasValue && !string.IsNullOrEmpty(request.EmployeeSignatureHmac))
            {
                if (!_integrityService.Verify("OnboardingRequest", request.Id.ToString(), request.EmployeeSignerName ?? "Employee", request.EmployeeSignedAt.Value, request.EmployeeSignature ?? "", request.EmployeeSignatureHmac))
                {
                    return ProcessResult<BinaryFilePayload>.Failure("Digital signature integrity check failed (Employee). The document may have been tampered with.", "Details", id);
                }
            }

            try
            {
                var pdfBytes = _pdfService.GeneratePdf(request);
                return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                    pdfBytes,
                    "application/pdf",
                    $"Onboarding_{request.FullName?.Replace(" ", "_") ?? "Draft"}_{request.RequestDate:yyyyMMdd}.pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF for request #{Id}", id);
                return ProcessResult<BinaryFilePayload>.Failure($"Failed to generate PDF: {ex.Message}", "Details", id);
            }
        }
    }
}
