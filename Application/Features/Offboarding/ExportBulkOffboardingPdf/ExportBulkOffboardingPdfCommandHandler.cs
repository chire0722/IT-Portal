// Project comment: ExportBulkOffboardingPdfCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf
{
    /// <summary>
    /// Handles the ExportBulkOffboardingPdfCommand command workflow.
    /// </summary>
    public sealed class ExportBulkOffboardingPdfCommandHandler : IRequestHandler<ExportBulkOffboardingPdfCommand, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPdfService _pdfService;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ILogger<ExportBulkOffboardingPdfCommandHandler> _logger;

        public ExportBulkOffboardingPdfCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, IPdfService pdfService, ISignatureIntegrityService integrityService, ILogger<ExportBulkOffboardingPdfCommandHandler> logger)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _pdfService = pdfService;
            _integrityService = integrityService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportBulkOffboardingPdfCommand request, CancellationToken cancellationToken)
        {
            if (request.Ids == null || request.Ids.Length == 0)
                return ProcessResult<BinaryFilePayload>.Failure("No requests selected.", "Index");

            var query = _context.OffboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOffboardingPdfData()
                .Where(r => request.Ids.Contains(r.Id));

            var user = _currentUserAccessor.User;
            var scopedCompanies = HrRecordReadCompanyScopeSupport.GetScopedCompanies(user);
            if (scopedCompanies.Count > 0)
            {
                query = CompanyScopeHelper.ApplyCompanyScope(query, scopedCompanies, item => item.Branch);
            }

            var requests = await query.ToListAsync(cancellationToken);
            if (!requests.Any())
                return ProcessResult<BinaryFilePayload>.Failure("No valid requests found.", "Index");

            foreach (var item in requests)
            {
                if (!SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", item.Id.ToString(), item.HRSignerName, item.HRSignedAt, item.HRSignature, item.HRSignatureHmac) ||
                    !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", item.Id.ToString(), item.HODSignerName, item.HODSignedAt, item.HODSignature, item.HODSignatureHmac) ||
                    !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", item.Id.ToString(), item.ITAdminSignerName, item.ITAdminSignedAt, item.ITAdminSignature, item.ITAdminSignatureHmac) ||
                    !SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", item.Id.ToString(), item.EmployeeSignerName, item.EmployeeSignedAt, item.EmployeeSignature, item.EmployeeSignatureHmac, "Employee"))
                {
                    return ProcessResult<BinaryFilePayload>.Failure(
                        $"Digital signature integrity validation failed for offboarding request #{item.Id}. The document cannot be exported as trusted.",
                        "Details",
                        item.Id);
                }
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var r in requests)
                {
                    try
                    {
                        var pdfBytes = _pdfService.GenerateOffboardingPdf(r);
                        var safeName = string.Join("_", r.FullName.Split(Path.GetInvalidFileNameChars()));
                        var entry = archive.CreateEntry($"Offboarding_{safeName}_ID{r.Id}.pdf", CompressionLevel.Fastest);
                        using var entryStream = entry.Open();
                        entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate PDF for Offboarding Request {RequestId}", r.Id);
                    }
                }
            }

            memoryStream.Position = 0;
            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                memoryStream.ToArray(),
                "application/zip",
                $"Offboarding_PDFs_{DateTime.Now:yyyyMMdd}.zip"));
        }
    }
}
