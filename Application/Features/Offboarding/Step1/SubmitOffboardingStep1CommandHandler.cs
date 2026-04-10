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
    public sealed class SubmitOffboardingStep1CommandHandler
        : IRequestHandler<SubmitOffboardingStep1Command, ProcessResult<OffboardingStep1PageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubmitOffboardingStep1CommandHandler(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
        }

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

            await OffboardingStep1Support.SaveDamageFileAsync(_env, existing, command.Id, command.HardwareDamageImage, cancellationToken);
            await OffboardingStep1Support.SaveReturnFileAsync(_env, existing, command.Id, command.HardwareReturnImage, cancellationToken);

            existing.UpdatedAt = DateTime.UtcNow;

            await OffboardingStep1Support.MarkReturnedAssetsAsync(_context, command.ReturnedAssetIds, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

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
