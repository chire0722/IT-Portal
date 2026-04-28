// Project comment: GetOffboardingStep2QueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    /// <summary>
    /// Handles the GetOffboardingStep2Query query workflow.
    /// </summary>
    public sealed class GetOffboardingStep2QueryHandler : IRequestHandler<GetOffboardingStep2Query, ProcessResult<OffboardingRequest>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetOffboardingStep2QueryHandler(
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
        public async Task<ProcessResult<OffboardingRequest>> Handle(GetOffboardingStep2Query request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests.FindAsync([request.Id], cancellationToken);
            if (offboardingRequest == null)
            {
                return ProcessResult<OffboardingRequest>.NotFound();
            }

            if (!await OffboardingActionWriteHelpers.IsUserAuthorizedForRequest(
                    _context,
                    _currentUserAccessor.User,
                    _userManager,
                    offboardingRequest,
                    _currentUserAccessor.UserName))
            {
                return ProcessResult<OffboardingRequest>.Forbidden();
            }

            if (offboardingRequest.Status != RequestStatus.PendingITApproval)
            {
                return ProcessResult<OffboardingRequest>.Failure("This request is not awaiting IT setup.", "Details", request.Id);
            }

            return ProcessResult<OffboardingRequest>.Success(offboardingRequest);
        }
    }
}
