// Project comment: GetOffboardingEmployeeDetailsQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Create
{
    /// <summary>
    /// Handles the GetOffboardingEmployeeDetailsQuery query workflow.
    /// </summary>
    public sealed class GetOffboardingEmployeeDetailsQueryHandler : IRequestHandler<GetOffboardingEmployeeDetailsQuery, OffboardingEmployeeDetailsViewModel?>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOffboardingEmployeeDetailsQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<OffboardingEmployeeDetailsViewModel?> Handle(GetOffboardingEmployeeDetailsQuery request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "HR"))
            {
                return null;
            }

            var onboardingRequest = await _context.OnboardingRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

            if (onboardingRequest == null ||
                !CompanyScopeHelper.CanAccessCompany(user, onboardingRequest.Branch))
            {
                return null;
            }

            var onboarding = onboardingRequest.Adapt<OffboardingEmployeeDetailsViewModel>();
            var currentHodEmail = await UserHelper.GetLatestHODEmailByCompanyAsync(_context, onboarding.Branch, onboarding.Department);
            onboarding.HODEmail = string.IsNullOrEmpty(currentHodEmail)
                ? onboarding.HODEmail
                : currentHodEmail;

            return onboarding;
        }
    }
}
