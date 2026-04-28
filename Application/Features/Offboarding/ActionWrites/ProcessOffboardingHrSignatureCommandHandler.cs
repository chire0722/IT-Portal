// Project comment: ProcessOffboardingHrSignatureCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOffboardingHrSignatureCommand command workflow.
    /// </summary>
    public sealed class ProcessOffboardingHrSignatureCommandHandler : IRequestHandler<ProcessOffboardingHrSignatureCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;

        public ProcessOffboardingHrSignatureCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager,
            ISignatureIntegrityService integrityService)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _integrityService = integrityService;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOffboardingHrSignatureCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "HR"))
                return new CommandResponse { Message = "Only the Requestor (HR) can sign this section.", RequestId = request.Id };

            var offboardingRequest = await _context.OffboardingRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
                return new CommandResponse { IsNotFound = true };

            if (!await OffboardingActionWriteHelpers.IsUserAuthorizedForRequest(_context, user, _userManager, offboardingRequest, _currentUserAccessor.UserName))
                return new CommandResponse { IsForbidden = true };

            var currentUser = await _userManager.GetUserAsync(user);
            var signedAt = DateTime.UtcNow;
            var signerName = !string.IsNullOrWhiteSpace(currentUser?.FullName) ? currentUser.FullName : (_currentUserAccessor.UserName ?? "Unknown");
            var signatureHmac = _integrityService.ComputeHmac("OffboardingRequest", offboardingRequest.Id.ToString(), signerName, signedAt, request.SignatureBase64);

            offboardingRequest.SignByHr(request.SignatureBase64, signerName, signatureHmac, signedAt);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse { IsSuccess = true, Message = "Requestor signature saved! Notification sent to the next approver.", RequestId = offboardingRequest.Id };
        }
    }
}
