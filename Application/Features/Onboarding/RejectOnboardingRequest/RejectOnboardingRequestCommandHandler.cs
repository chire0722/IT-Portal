using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.RejectOnboardingRequest
{
    public sealed class RejectOnboardingRequestCommandHandler : IRequestHandler<RejectOnboardingRequestCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<RejectOnboardingRequestCommandHandler> _logger;

        public RejectOnboardingRequestCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            ILogger<RejectOnboardingRequestCommandHandler> logger)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _logger = logger;
        }

        public async Task<ProcessResult<object>> Handle(RejectOnboardingRequestCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "Manager") && !user.HasClaim("UserTitle", "IT"))
            {
                return ProcessResult<object>.Failure("Only Managers or IT Admins can reject requests.", "Details", request.Id, ajaxAware: true);
            }

            var onboardingRequest = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (onboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            var currentUsername = _currentUserAccessor.UserName;
            if (!CompanyScopeHelper.CanAccessCompany(user, onboardingRequest.Branch))
            {
                return ProcessResult<object>.Forbidden();
            }

            if (!onboardingRequest.CanBeRejected())
            {
                return ProcessResult<object>.Failure("Cannot reject a request that is already approved or completed.", "Details", request.Id, ajaxAware: true);
            }

            try
            {
                onboardingRequest.Reject(request.Reason, currentUsername, DateTime.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Reject attempted with an invalid onboarding state transition for request #{Id} in status {Status}.",
                    request.Id,
                    onboardingRequest.Status);
                return ProcessResult<object>.Failure(
                    "Cannot reject a request from its current state.",
                    "Details",
                    request.Id,
                    ajaxAware: true);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Request #{Id} rejected by {User}. Reason: {Reason}", request.Id, currentUsername, request.Reason);

            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = "Request has been rejected.",
                RedirectAction = "Details",
                RequestId = onboardingRequest.Id,
                AjaxAware = true,
                AutoOpenEmail = $"ComposeRejectionEmail:{onboardingRequest.Id}:Onboarding"
            };
        }
    }
}
