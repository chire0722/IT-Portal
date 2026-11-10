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

            if (user?.HasClaim("UserTitle", "HR") != true)
            {
                return ProcessResult<BinaryFilePayload>.Forbidden();
            }

            var requestedIds = ids.Where(id => id > 0).Distinct().ToArray();
            if (requestedIds.Length == 0 || requestedIds.Length != ids.Length)
            {
                return ProcessResult<BinaryFilePayload>.Failure("Select valid requests before exporting.", "Index");
            }

            var requests = await _context.OnboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOnboardingPdfData()
                .Where(r => requestedIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            requests = requests
                .Where(request => HrRecordReadCompanyScopeSupport.CanAccessCompany(user, request.Branch))
                .ToList();

            // Never silently export an in-scope subset of a mixed-company selection.
            if (requests.Count != requestedIds.Length)
            {
                return ProcessResult<BinaryFilePayload>.NotFound();
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var request in requests)
                {
                    try
                    {
                        if (!AreSignaturesValid(request))
                        {
                            return ProcessResult<BinaryFilePayload>.Failure(
                                "One or more digital signatures failed integrity validation. The export was cancelled.",
                                "Index");
                        }

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

        private bool AreSignaturesValid(IT_Form.Models.OnboardingRequest request)
            => SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", request.Id.ToString(), request.RequesterSignerName, "Unknown", request.RequesterSignedAt, request.RequesterSignature, request.RequesterSignatureHmac)
               && SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", request.Id.ToString(), request.ITAdminSignerName, "Unknown", request.ITAdminSignedAt, request.ITAdminSignature, request.ITAdminSignatureHmac)
               && SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", request.Id.ToString(), request.EmployeeSignerName, "Employee", request.EmployeeSignedAt, request.EmployeeSignature, request.EmployeeSignatureHmac);

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

            if (!AreSignaturesValid(request))
            {
                return ProcessResult<BinaryFilePayload>.Failure(
                    "Digital signature integrity check failed. The document may have been tampered with.",
                    "Details",
                    id);
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
