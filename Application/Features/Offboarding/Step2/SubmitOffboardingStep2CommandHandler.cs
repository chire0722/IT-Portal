// Project comment: SubmitOffboardingStep2CommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Data;
using IT_Form.Models;
using Microsoft.AspNetCore.Identity;
using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    /// <summary>
    /// Handles the SubmitOffboardingStep2Command command workflow.
    /// </summary>
    public sealed class SubmitOffboardingStep2CommandHandler : IRequestHandler<SubmitOffboardingStep2Command, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubmitOffboardingStep2CommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(SubmitOffboardingStep2Command request, CancellationToken cancellationToken)
        {
            var existing = await _context.OffboardingRequests.FindAsync([request.Id], cancellationToken);
            if (existing == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (!await OffboardingActionWriteHelpers.IsUserAuthorizedForRequest(
                    _context,
                    _currentUserAccessor.User,
                    _userManager,
                    existing,
                    _currentUserAccessor.UserName))
            {
                return ProcessResult<object>.Forbidden();
            }

            if (!existing.CanEditItChecklist())
            {
                return ProcessResult<object>.Failure("Cannot modify checklist - this request has already been signed.", "Details", request.Id);
            }

            existing.ApplyStep2Checklist(request.Request, DateTime.UtcNow);

            if (string.Equals(request.SaveAction, "submit", StringComparison.OrdinalIgnoreCase))
            {
                await _context.LogActionAsync("OffboardingRequest", existing.Id.ToString(), "Update", "IT Setup Step 2 completed.");
                await _context.SaveChangesAsync(cancellationToken);
                return ProcessResult<object>.Success(
                    message: "Offboarding IT Setup completed successfully. Please review and sign the request when ready.",
                    redirectAction: "Details",
                    requestId: request.Id);
            }

            await _context.LogActionAsync("OffboardingRequest", existing.Id.ToString(), "Update", "IT Setup Step 2 draft saved.");
            await _context.SaveChangesAsync(cancellationToken);
            return ProcessResult<object>.Success(
                message: "Draft saved successfully.",
                redirectAction: "Step2",
                requestId: request.Id);
        }
    }
}
