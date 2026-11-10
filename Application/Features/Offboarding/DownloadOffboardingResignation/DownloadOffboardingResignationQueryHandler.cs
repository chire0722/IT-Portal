// Project comment: DownloadOffboardingResignationQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Application.Features.Offboarding;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.DownloadOffboardingResignation
{
    /// <summary>
    /// Handles the DownloadOffboardingResignationQuery query workflow.
    /// </summary>
    public sealed class DownloadOffboardingResignationQueryHandler : IRequestHandler<DownloadOffboardingResignationQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public DownloadOffboardingResignationQueryHandler(ApplicationDbContext context, IWebHostEnvironment environment, ICurrentUserAccessor currentUserAccessor, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(DownloadOffboardingResignationQuery request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null || string.IsNullOrEmpty(offboardingRequest.ResignationLetterPath))
                return ProcessResult<BinaryFilePayload>.NotFound();

            if (!await OffboardingReadAccessSupport.IsUserAuthorizedForRequest(_context, _currentUserAccessor.User, _userManager, offboardingRequest, _currentUserAccessor.UserName))
                return ProcessResult<BinaryFilePayload>.NotFound();

            await _context.LogActionAsync(nameof(OffboardingRequest), request.Id.ToString(), "Read", $"Read resignation letter for: {offboardingRequest.FullName}");
            var filePath = SensitiveFileStorage.ResolvePhysicalPath(_environment, offboardingRequest.ResignationLetterPath);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return ProcessResult<BinaryFilePayload>.NotFound();

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                content,
                contentType,
                Path.GetFileName(filePath)));
        }
    }
}
