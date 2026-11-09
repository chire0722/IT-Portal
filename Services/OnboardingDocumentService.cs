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

            var invalidRequest = requests.FirstOrDefault(request => VerifyIntegrity(request) != null);
            if (invalidRequest != null)
            {
                return ProcessResult<BinaryFilePayload>.Failure(
                    $"Digital signature integrity validation failed for onboarding request #{invalidRequest.Id}. The document cannot be exported as trusted.",
                    "Details",
                    invalidRequest.Id);
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

            var integrityError = VerifyIntegrity(request);
            if (integrityError != null)
            {
                return ProcessResult<BinaryFilePayload>.Failure(integrityError, "Details", id);
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

        private string? VerifyIntegrity(Models.OnboardingRequest request)
        {
            var entityId = request.Id.ToString();
            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                    request.RequesterSignerName, request.RequesterSignedAt, request.RequesterSignature, request.RequesterSignatureHmac))
                return "Requester (HR) signature integrity validation failed. The document may have been tampered with or uses a compromised legacy key.";

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                    request.ITAdminSignerName, request.ITAdminSignedAt, request.ITAdminSignature, request.ITAdminSignatureHmac))
                return "IT Admin signature integrity validation failed. The document may have been tampered with or uses a compromised legacy key.";

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                    request.EmployeeSignerName, request.EmployeeSignedAt, request.EmployeeSignature, request.EmployeeSignatureHmac, "Employee"))
                return "Employee signature integrity validation failed. The document may have been tampered with or uses a compromised legacy key.";

            return null;
        }
    }
}
