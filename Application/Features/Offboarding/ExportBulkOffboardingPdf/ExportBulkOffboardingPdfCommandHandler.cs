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
        private readonly ILogger<ExportBulkOffboardingPdfCommandHandler> _logger;

        public ExportBulkOffboardingPdfCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, IPdfService pdfService, ILogger<ExportBulkOffboardingPdfCommandHandler> logger)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _pdfService = pdfService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        private const int MaxSelectedRequests = 100;
        private const long MaxTotalPdfBytes = 100L * 1024 * 1024;

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportBulkOffboardingPdfCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (user?.HasClaim("UserTitle", "HR") != true)
            {
                return ProcessResult<BinaryFilePayload>.Forbidden();
            }

            var requestedIds = request.Ids?
                .Where(id => id > 0)
                .Distinct()
                .ToArray() ?? [];

            if (requestedIds.Length == 0)
            {
                return ProcessResult<BinaryFilePayload>.Failure("No requests selected.", "Index");
            }

            if (requestedIds.Length > MaxSelectedRequests)
            {
                return ProcessResult<BinaryFilePayload>.Failure(
                    $"Select no more than {MaxSelectedRequests} requests per export.",
                    "Index");
            }

            var query = _context.OffboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOffboardingPdfData()
                .Where(r => requestedIds.Contains(r.Id));

            var scopedCompanies = HrRecordReadCompanyScopeSupport.GetScopedCompanies(user);
            if (scopedCompanies.Count > 0)
            {
                query = CompanyScopeHelper.ApplyCompanyScope(query, scopedCompanies, item => item.Branch);
            }

            var requests = await query.ToListAsync(cancellationToken);

            // Never silently export only the in-scope subset of a mixed-tenant selection.
            if (requests.Count != requestedIds.Length)
            {
                return ProcessResult<BinaryFilePayload>.NotFound();
            }

            try
            {
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    long totalPdfBytes = 0;
                    foreach (var requestItem in requests)
                    {
                        var pdfBytes = _pdfService.GenerateOffboardingPdf(requestItem);
                        totalPdfBytes += pdfBytes.LongLength;
                        if (totalPdfBytes > MaxTotalPdfBytes)
                        {
                            return ProcessResult<BinaryFilePayload>.Failure(
                                "The selected export exceeds the permitted size. Reduce the number of requests and retry.",
                                "Index");
                        }

                        var invalidFileNameCharacters = Path.GetInvalidFileNameChars()
                            .Concat(new[] { '/', '\\\\' })
                            .ToHashSet();
                        var safeName = new string(requestItem.FullName
                            .Select(character => char.IsControl(character) || invalidFileNameCharacters.Contains(character)
                                ? '_'
                                : character)
                            .ToArray())
                            .Trim(' ', '.', '_');
                        if (string.IsNullOrWhiteSpace(safeName))
                        {
                            safeName = "Employee";
                        }

                        var entry = archive.CreateEntry(
                            $"Offboarding_{safeName}_ID{requestItem.Id}.pdf",
                            CompressionLevel.Fastest);
                        using var entryStream = entry.Open();
                        entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                    }
                }

                return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                    memoryStream.ToArray(),
                    "application/zip",
                    $"Offboarding_PDFs_{DateTime.UtcNow:yyyyMMdd}.zip"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate the requested bulk offboarding PDF export.");
                return ProcessResult<BinaryFilePayload>.Failure(
                    "Unable to generate the requested export. Please retry or contact IT.",
                    "Index");
            }
        }
    }
}
