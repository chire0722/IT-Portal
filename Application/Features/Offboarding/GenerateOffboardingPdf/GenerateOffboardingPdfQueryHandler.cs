// Project comment: GenerateOffboardingPdfQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Application.Features.Offboarding;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.Offboarding.GenerateOffboardingPdf
{
    /// <summary>
    /// Handles the GenerateOffboardingPdfQuery query workflow.
    /// </summary>
    public sealed class GenerateOffboardingPdfQueryHandler : IRequestHandler<GenerateOffboardingPdfQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPdfService _pdfService;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ILogger<GenerateOffboardingPdfQueryHandler> _logger;

        public GenerateOffboardingPdfQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, UserManager<ApplicationUser> userManager, IPdfService pdfService, ISignatureIntegrityService integrityService, ILogger<GenerateOffboardingPdfQueryHandler> logger)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _pdfService = pdfService;
            _integrityService = integrityService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(GenerateOffboardingPdfQuery request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .IncludeOffboardingPdfData()
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (offboardingRequest == null)
                return ProcessResult<BinaryFilePayload>.NotFound();

            if (!await OffboardingReadAccessSupport.IsUserAuthorizedForRequest(_context, _currentUserAccessor.User, _userManager, offboardingRequest, _currentUserAccessor.UserName))
                return ProcessResult<BinaryFilePayload>.Forbidden();

            if (offboardingRequest.HRSignedAt.HasValue && !string.IsNullOrEmpty(offboardingRequest.HRSignatureHmac) &&
                !_integrityService.Verify("OffboardingRequest", offboardingRequest.Id.ToString(), offboardingRequest.HRSignerName ?? "Unknown", offboardingRequest.HRSignedAt.Value, offboardingRequest.HRSignature ?? "", offboardingRequest.HRSignatureHmac))
                return ProcessResult<BinaryFilePayload>.Failure("Digital signature integrity check failed (HR). The document may have been tampered with.", "Details", request.Id);

            if (offboardingRequest.HODSignedAt.HasValue && !string.IsNullOrEmpty(offboardingRequest.HODSignatureHmac) &&
                !_integrityService.Verify("OffboardingRequest", offboardingRequest.Id.ToString(), offboardingRequest.HODSignerName ?? "Unknown", offboardingRequest.HODSignedAt.Value, offboardingRequest.HODSignature ?? "", offboardingRequest.HODSignatureHmac))
                return ProcessResult<BinaryFilePayload>.Failure("Digital signature integrity check failed (HOD). The document may have been tampered with.", "Details", request.Id);

            if (offboardingRequest.ITAdminSignedAt.HasValue && !string.IsNullOrEmpty(offboardingRequest.ITAdminSignatureHmac) &&
                !_integrityService.Verify("OffboardingRequest", offboardingRequest.Id.ToString(), offboardingRequest.ITAdminSignerName ?? "Unknown", offboardingRequest.ITAdminSignedAt.Value, offboardingRequest.ITAdminSignature ?? "", offboardingRequest.ITAdminSignatureHmac))
                return ProcessResult<BinaryFilePayload>.Failure("Digital signature integrity check failed (IT Admin). The document may have been tampered with.", "Details", request.Id);

            if (offboardingRequest.EmployeeSignedAt.HasValue && !string.IsNullOrEmpty(offboardingRequest.EmployeeSignatureHmac) &&
                !_integrityService.Verify("OffboardingRequest", offboardingRequest.Id.ToString(), offboardingRequest.EmployeeSignerName ?? "Employee", offboardingRequest.EmployeeSignedAt.Value, offboardingRequest.EmployeeSignature ?? "", offboardingRequest.EmployeeSignatureHmac))
                return ProcessResult<BinaryFilePayload>.Failure("Digital signature integrity check failed (Employee). The document may have been tampered with.", "Details", request.Id);

            try
            {
                var pdfBytes = _pdfService.GenerateOffboardingPdf(offboardingRequest);
                return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                    pdfBytes,
                    "application/pdf",
                    $"Offboarding_{offboardingRequest.FullName.Replace(" ", "_")}_{offboardingRequest.RequestDate:yyyyMMdd}.pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF for offboarding request #{Id}", request.Id);
                return ProcessResult<BinaryFilePayload>.Failure($"Failed to generate PDF: {ex.Message}", "Details", request.Id);
            }
        }
    }
}
