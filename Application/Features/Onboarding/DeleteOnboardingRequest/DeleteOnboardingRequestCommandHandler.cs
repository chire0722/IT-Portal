// Project comment: DeleteOnboardingRequestCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest
{
    /// <summary>
    /// Handles the DeleteOnboardingRequestCommand command workflow.
    /// </summary>
    public sealed class DeleteOnboardingRequestCommandHandler
        : IRequestHandler<DeleteOnboardingRequestCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<DeleteOnboardingRequestCommandHandler> _logger;

        public DeleteOnboardingRequestCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            ILogger<DeleteOnboardingRequestCommandHandler> logger)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(DeleteOnboardingRequestCommand request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            var user = _currentUserAccessor.User;
            var currentUsername = _currentUserAccessor.UserName;
            var isHr = user.HasClaim("UserTitle", PortalTitles.HR);
            var isIt = PortalTitles.IsPrivilegedIt(user);

            // Enforce the HR/IT role boundary in the handler as well as the API endpoint.
            // This protects other callers that dispatch the command directly through MediatR.
            if (!isHr && !isIt)
            {
                return ProcessResult<object>.Forbidden();
            }

            if (!CompanyScopeHelper.CanAccessCompany(user, onboardingRequest.Branch))
            {
                return ProcessResult<object>.Forbidden();
            }

            if (!onboardingRequest.CanBeDeletedBy(isHr))
            {
                return ProcessResult<object>.Failure(
                    "Only Draft, Rejected, or HR-signed (awaiting others) requests can be deleted.",
                    "Index",
                    ajaxAware: true);
            }

            await _context.LogActionAsync(
                nameof(OnboardingRequest),
                request.Id.ToString(),
                "Deleted",
                $"Onboarding form for {onboardingRequest.FullName} was deleted by {currentUsername}. Status was: {onboardingRequest.Status}");

            var employeeName = onboardingRequest.FullName;
            _context.OnboardingRequests.Remove(onboardingRequest);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Onboarding request #{Id} for {Name} deleted.", request.Id, employeeName);

            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = $"Request for {employeeName} has been deleted.",
                RedirectAction = "Index",
                AjaxAware = true
            };
        }
    }
}
