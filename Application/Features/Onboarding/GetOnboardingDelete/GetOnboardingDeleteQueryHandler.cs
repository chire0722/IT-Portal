// Project comment: GetOnboardingDeleteQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDelete
{
    /// <summary>
    /// Handles the GetOnboardingDeleteQuery query workflow.
    /// </summary>
    public sealed class GetOnboardingDeleteQueryHandler : IRequestHandler<GetOnboardingDeleteQuery, ProcessResult<OnboardingRequest>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOnboardingDeleteQueryHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<OnboardingRequest>> Handle(GetOnboardingDeleteQuery request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return ProcessResult<OnboardingRequest>.NotFound();
            }

            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, onboardingRequest.Branch))
            {
                return ProcessResult<OnboardingRequest>.Forbidden();
            }

            if (!onboardingRequest.CanBeDeletedBy(isHr: false))
            {
                return ProcessResult<OnboardingRequest>.Failure("Only Draft or Rejected requests can be deleted.", "Index");
            }

            return ProcessResult<OnboardingRequest>.Success(onboardingRequest);
        }
    }
}
