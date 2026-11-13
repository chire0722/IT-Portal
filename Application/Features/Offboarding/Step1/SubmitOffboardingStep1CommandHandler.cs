// Project comment: SubmitOffboardingStep1CommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    /// <summary>
    /// Handles the SubmitOffboardingStep1Command command workflow.
    /// </summary>
    public sealed class SubmitOffboardingStep1CommandHandler
        : IRequestHandler<SubmitOffboardingStep1Command, ProcessResult<OffboardingStep1PageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileMalwareScanner? _malwareScanner;

        public SubmitOffboardingStep1CommandHandler(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager,
            IFileMalwareScanner? malwareScanner = null)
        {
            _context = context;
            _env = env;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _malwareScanner = malwareScanner;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<OffboardingStep1PageViewModel>> Handle(
            SubmitOffboardingStep1Command command,
            CancellationToken cancellationToken)
        {
            var existing = await _context.OffboardingRequests.FindAsync([command.Id], cancellationToken);
            if (existing == null)
            {
                return ProcessResult<OffboardingStep1PageViewModel>.NotFound();
            }

            if (!await OffboardingActionWriteHelpers.IsUserAuthorizedForRequest(
                    _context,
                    _currentUserAccessor.User,
                    _userManager,
                    existing,
                    _currentUserAccessor.UserName))
            {
                return ProcessResult<OffboardingStep1PageViewModel>.Forbidden();
            }

            if (existing.Status == RequestStatus.PendingEmployeeSign ||
                existing.Status == RequestStatus.Completed ||
                existing.Status == RequestStatus.Rejected)
            {
                return ProcessResult<OffboardingStep1PageViewModel>.Failure(
                    "This request cannot be edited in its current status.",
                    "Details",
                    command.Id);
            }

            OffboardingStep1Support.ApplyStep1Fields(existing, command.Request);

            var damageValidation = OffboardingStep1Support.ValidateDamageFile(command.HardwareDamageImage);
            if (damageValidation != null)
            {
                return await OffboardingStep1Support.FailureAsync(_context, damageValidation, command.Request, cancellationToken);
            }

            var returnValidation = OffboardingStep1Support.ValidateReturnFile(command.HardwareReturnImage);
            if (returnValidation != null)
            {
                return await OffboardingStep1Support.FailureAsync(_context, returnValidation, command.Request, cancellationToken);
            }

            // Validate every binary ingress before it reaches managed storage. The signature
            // and extension checks above are not malware detection and must not be treated as such.
            await MalwareScanHelper.EnsureCleanAsync(
                command.HardwareDamageImage,
                _malwareScanner,
                "offboarding hardware-damage image",
                cancellationToken);
            await MalwareScanHelper.EnsureCleanAsync(
                command.HardwareReturnImage,
                _malwareScanner,
                "offboarding hardware-return image",
                cancellationToken);

            // Reconcile the employee/company assignment before writing new files. This keeps
            // a rejected asset selection from leaving an orphaned sensitive upload behind.
            var returnedAssetError = await OffboardingStep1Support.MarkReturnedAssetsAsync(
                _context,
                existing,
                command.ReturnedAssetIds,
                cancellationToken);
            if (returnedAssetError != null)
            {
                return await OffboardingStep1Support.FailureAsync(
                    _context,
                    returnedAssetError,
                    command.Request,
                    cancellationToken);
            }

            var previousDamageFilePath = await OffboardingStep1Support.SaveDamageFileAsync(
                _env, existing, command.Id, command.HardwareDamageImage, cancellationToken);
            var previousReturnFilePath = await OffboardingStep1Support.SaveReturnFileAsync(
                _env, existing, command.Id, command.HardwareReturnImage, cancellationToken);

            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Delete old files only after the new file references have been persisted.
            SensitiveFileStorage.DeleteStoredFile(_env, previousDamageFilePath);
            SensitiveFileStorage.DeleteStoredFile(_env, previousReturnFilePath);

            if (string.Equals(command.SaveAction, "draft", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessResult<OffboardingStep1PageViewModel>.Success(
                    message: "Draft saved successfully.",
                    redirectAction: "Step1",
                    requestId: command.Id);
            }

            return ProcessResult<OffboardingStep1PageViewModel>.Success(
                redirectAction: "Step2",
                requestId: command.Id);
        }
    }
}

