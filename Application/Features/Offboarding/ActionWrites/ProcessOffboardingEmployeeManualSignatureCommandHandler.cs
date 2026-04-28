// Project comment: ProcessOffboardingEmployeeManualSignatureCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOffboardingEmployeeManualSignatureCommand command workflow.
    /// </summary>
    public sealed class ProcessOffboardingEmployeeManualSignatureCommandHandler : IRequestHandler<ProcessOffboardingEmployeeManualSignatureCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;

        public ProcessOffboardingEmployeeManualSignatureCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, UserManager<ApplicationUser> userManager, ISignatureIntegrityService integrityService)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _integrityService = integrityService;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOffboardingEmployeeManualSignatureCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "HR"))
                return new CommandResponse { Message = "Only HR can manually sign on behalf of the employee.", RequestId = request.Id };

            var offboardingRequest = await _context.OffboardingRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
                return new CommandResponse { IsNotFound = true };

            if (!await OffboardingActionWriteHelpers.IsUserAuthorizedForRequest(_context, user, _userManager, offboardingRequest, _currentUserAccessor.UserName))
                return new CommandResponse { IsForbidden = true };

            if (offboardingRequest.Status != RequestStatus.PendingEmployeeSign)
                return new CommandResponse { Message = "Request is not awaiting employee signature.", RequestId = request.Id };

            offboardingRequest.EmployeeSignature = request.SignatureBase64;
            offboardingRequest.EmployeeSignedAt = DateTime.UtcNow;
            offboardingRequest.EmployeeSignerName = offboardingRequest.FullName ?? "Employee";
            offboardingRequest.EmployeeSignatureHmac = _integrityService.ComputeHmac("OffboardingRequest", offboardingRequest.Id.ToString(), offboardingRequest.EmployeeSignerName, offboardingRequest.EmployeeSignedAt.Value, request.SignatureBase64);
            offboardingRequest.Status = RequestStatus.Completed;
            offboardingRequest.CompletedAt = DateTime.UtcNow;
            offboardingRequest.UpdatedAt = DateTime.UtcNow;

            if (offboardingRequest.OnboardingRequestId.HasValue)
            {
                var keys = await _context.SoftwareKeys
                    .Where(k => k.OnboardingRequestId == offboardingRequest.OnboardingRequestId.Value && k.IsAssigned)
                    .ToListAsync(cancellationToken);

                foreach (var key in keys)
                {
                    key.IsAssigned = false;
                    key.OnboardingRequestId = null;
                    key.AssignedToUserId = null;
                    key.MachineName = null;
                    key.AssignmentDate = null;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new CommandResponse { IsSuccess = true, Message = "Employee manual signature verified. Offboarding complete!", RequestId = offboardingRequest.Id };
        }
    }
}
