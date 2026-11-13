// Project comment: RegenerateOnboardingTokenCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken
{
    /// <summary>
    /// Handles the RegenerateOnboardingTokenCommand command workflow.
    /// </summary>
    public sealed class RegenerateOnboardingTokenCommandHandler : IRequestHandler<RegenerateOnboardingTokenCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public RegenerateOnboardingTokenCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(RegenerateOnboardingTokenCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "HR"))
            {
                return ProcessResult<object>.Forbidden();
            }

            var onboardingRequest = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (onboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (!CompanyScopeHelper.CanAccessCompany(user, onboardingRequest.Branch))
            {
                return ProcessResult<object>.Forbidden();
            }

            if (!onboardingRequest.CanRegenerateToken())
            {
                return ProcessResult<object>.Failure("Token can only be regenerated for requests awaiting employee signature.", "Details", onboardingRequest.Id, ajaxAware: true);
            }

            onboardingRequest.RegenerateAccessToken(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = "Signature link has been regenerated. The old link is now invalid.",
                RedirectAction = "Details",
                RequestId = onboardingRequest.Id,
                AjaxAware = true
            };
        }
    }
}

