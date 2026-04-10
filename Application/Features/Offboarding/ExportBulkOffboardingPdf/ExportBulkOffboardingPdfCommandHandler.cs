using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf
{
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

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportBulkOffboardingPdfCommand request, CancellationToken cancellationToken)
        {
            if (request.Ids == null || request.Ids.Length == 0)
                return ProcessResult<BinaryFilePayload>.Failure("No requests selected.", "Index");

            var query = _context.OffboardingRequests
                .AsNoTracking()
                .Include(o => o.OnboardingRequest).ThenInclude(or => or!.IssuedAssets)
                .Include(o => o.OnboardingRequest).ThenInclude(or => or!.FolderPermissions)
                .Where(r => request.Ids.Contains(r.Id));

            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "IT") && !user.HasClaim("UserTitle", "HR"))
            {
                var userCompanies = UserHelper.GetClaimValues(user, "UserCompany");
                if (userCompanies.Any())
                {
                    var currentUserName = _currentUserAccessor.UserName;
                    query = query.Where(r =>
                        (r.Branch != null && userCompanies.Any(c =>
                            c == r.Branch ||
                            c.StartsWith(r.Branch + " (") ||
                            r.Branch.StartsWith(c + " (")))
                        || r.CreatedByUserId == currentUserName);
                }
            }

            var requests = await query.ToListAsync(cancellationToken);
            if (!requests.Any())
                return ProcessResult<BinaryFilePayload>.Failure("No valid requests found.", "Index");

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
