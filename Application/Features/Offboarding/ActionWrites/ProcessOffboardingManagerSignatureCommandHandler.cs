// Project comment: ProcessOffboardingManagerSignatureCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOffboardingManagerSignatureCommand command workflow.
    /// </summary>
    public sealed class ProcessOffboardingManagerSignatureCommandHandler : IRequestHandler<ProcessOffboardingManagerSignatureCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;

        public ProcessOffboardingManagerSignatureCommandHandler(
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
        public async Task<CommandResponse> Handle(ProcessOffboardingManagerSignatureCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "HOD"))
                return new CommandResponse { Message = "Only Head of Department can sign this section.", RequestId = request.Id };

            var offboardingRequest = await _context.OffboardingRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
                return new CommandResponse { IsNotFound = true };

            if (!CompanyScopeHelper.CanAccessCompany(user, offboardingRequest.Branch))
                return new CommandResponse { IsForbidden = true, RequestId = request.Id };

            var currentUser = await _userManager.GetUserAsync(user);
            var userId = _userManager.GetUserId(user);
            if (string.IsNullOrWhiteSpace(userId))
                userId = currentUser?.Id;

            var hodEmails = await UserHelper.GetHODEmailsAsync(_context, _currentUserAccessor.UserName ?? string.Empty);
            var currentUserEmail = currentUser?.Email?.ToLowerInvariant();
            var assignedMatch = !string.IsNullOrWhiteSpace(offboardingRequest.AssignedHodId) &&
                                !string.IsNullOrWhiteSpace(userId) &&
                                offboardingRequest.AssignedHodId == userId;
            var emailMatch = offboardingRequest.HODEmail != null &&
                             (hodEmails.Contains(offboardingRequest.HODEmail.ToLowerInvariant()) ||
                              (!string.IsNullOrWhiteSpace(currentUserEmail) &&
                               string.Equals(offboardingRequest.HODEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase)));
            if (!offboardingRequest.CanBeSignedByManager(assignedMatch, emailMatch))
                return new CommandResponse { Message = "You are not authorized to sign as the Head of Department for this request.", RequestId = request.Id };

            var signedAt = DateTime.UtcNow;
            var signerName = !string.IsNullOrWhiteSpace(currentUser?.FullName) ? currentUser.FullName : (_currentUserAccessor.UserName ?? "Unknown");
            var signatureHmac = _integrityService.ComputeHmac("OffboardingRequest", offboardingRequest.Id.ToString(), signerName, signedAt, request.SignatureBase64);

            offboardingRequest.SignByManager(request.SignatureBase64, signerName, signatureHmac, signedAt);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse { IsSuccess = true, Message = "Manager signature saved! Notification sent to the IT Department.", RequestId = offboardingRequest.Id };
        }
    }
}
