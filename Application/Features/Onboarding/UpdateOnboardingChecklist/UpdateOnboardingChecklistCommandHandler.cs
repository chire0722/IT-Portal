// Project comment: UpdateOnboardingChecklistCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist
{
    /// <summary>
    /// Handles the UpdateOnboardingChecklistCommand command workflow.
    /// </summary>
    public sealed class UpdateOnboardingChecklistCommandHandler : IRequestHandler<UpdateOnboardingChecklistCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;

        public UpdateOnboardingChecklistCommandHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(UpdateOnboardingChecklistCommand request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (onboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (!onboardingRequest.CanBeEdited())
            {
                return ProcessResult<object>.Failure("Cannot modify checklist - this request has already been signed.", "Details", request.Id, ajaxAware: true);
            }

            onboardingRequest.ApplyItChecklist(
                request.CheckPhysicalClean,
                request.CheckO365Setup,
                request.CheckWindowsLogin,
                request.CheckEmailSetup,
                request.CheckSpecificSoftware,
                request.CheckGeneralSoftware,
                request.CheckAbcGroup,
                request.CheckEmailMailGroup,
                request.CheckDomainJoin,
                request.CheckSharedDrive,
                request.CheckWiFiSetup,
                request.CheckPrinterSetup,
                request.CheckScanFolder,
                request.CheckVPNSetup,
                request.ChecklistRemarks,
                DateTime.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);

            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = "IT Checklist updated successfully.",
                RedirectAction = "Details",
                RequestId = onboardingRequest.Id,
                AjaxAware = true
            };
        }
    }
}
