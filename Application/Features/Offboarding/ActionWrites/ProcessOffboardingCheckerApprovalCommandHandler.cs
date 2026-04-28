// Project comment: ProcessOffboardingCheckerApprovalCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOffboardingCheckerApprovalCommand command workflow.
    /// </summary>
    public sealed class ProcessOffboardingCheckerApprovalCommandHandler
        : IRequestHandler<ProcessOffboardingCheckerApprovalCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISender _sender;

        public ProcessOffboardingCheckerApprovalCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager,
            ISender sender)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _sender = sender;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOffboardingCheckerApprovalCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "HOD"))
            {
                return new CommandResponse { Message = "Only Head of Department can approve as checker.", RequestId = request.Id };
            }

            var offboardingRequest = await _context.OffboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
            {
                return new CommandResponse { IsNotFound = true };
            }

            var username = _currentUserAccessor.UserName ?? string.Empty;
            if (!offboardingRequest.CanBeCheckerApprovedBy(username))
            {
                return new CommandResponse { Message = "Maker and checker must be different users.", RequestId = request.Id };
            }

            var approverId = _userManager.GetUserId(user);
            if (string.IsNullOrWhiteSpace(approverId))
            {
                approverId = (await _userManager.GetUserAsync(user))?.Id;
            }

            offboardingRequest.ApproveChecker(approverId, username, DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            return await _sender.Send(
                new ProcessOffboardingManagerSignatureCommand(request.Id, request.SignatureBase64),
                cancellationToken);
        }
    }
}
