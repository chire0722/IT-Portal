using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.RejectOffboardingRequest
{
    public sealed class RejectOffboardingRequestCommandHandler : IRequestHandler<RejectOffboardingRequestCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public RejectOffboardingRequestCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
        }

        public async Task<ProcessResult<object>> Handle(RejectOffboardingRequestCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "Manager") && !user.HasClaim("UserTitle", "IT"))
            {
                return ProcessResult<object>.Failure("Only Managers or IT Admins can reject requests.", "Details", request.Id, ajaxAware: true);
            }

            var offboardingRequest = await _context.OffboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (!CompanyScopeHelper.CanAccessCompany(user, offboardingRequest.Branch))
            {
                return ProcessResult<object>.Forbidden();
            }

            if (!await IsUserAuthorizedForRequest(offboardingRequest))
            {
                return ProcessResult<object>.Forbidden();
            }

            if (!offboardingRequest.CanBeRejected())
            {
                return ProcessResult<object>.Failure("Cannot reject a request that is already approved or completed.", "Details", request.Id, ajaxAware: true);
            }

            offboardingRequest.Reject(request.Reason, _currentUserAccessor.UserName, DateTime.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);

            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = "Request has been rejected.",
                RedirectAction = "Details",
                RequestId = offboardingRequest.Id,
                AjaxAware = true,
                AutoOpenEmail = $"ComposeRejectionEmail:{offboardingRequest.Id}:Offboarding"
            };
        }

        private async Task<bool> IsUserAuthorizedForRequest(OffboardingRequest request)
        {
            var user = _currentUserAccessor.User;
            if (user.HasClaim("UserTitle", "IT")) return true;
            if (user.HasClaim("UserTitle", "HR")) return true;

            var username = _currentUserAccessor.UserName ?? string.Empty;
            if (request.CreatedByUserId != null && request.CreatedByUserId == username) return true;

            var userId = _userManager.GetUserId(user);
            if (request.AssignedHodId != null)
            {
                return request.AssignedHodId == userId;
            }

            var hodEmailsLower = await UserHelper.GetHODEmailsAsync(_context, username);
            if (request.HODEmail != null && hodEmailsLower.Contains(request.HODEmail.ToLowerInvariant()))
            {
                return true;
            }

            return false;
        }
    }
}
