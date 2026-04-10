using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Create
{
    public sealed class GetOffboardingEmployeeDetailsQueryHandler : IRequestHandler<GetOffboardingEmployeeDetailsQuery, OffboardingEmployeeDetailsViewModel?>
    {
        private readonly ApplicationDbContext _context;

        public GetOffboardingEmployeeDetailsQueryHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OffboardingEmployeeDetailsViewModel?> Handle(GetOffboardingEmployeeDetailsQuery request, CancellationToken cancellationToken)
        {
            var onboarding = await _context.OnboardingRequests
                .AsNoTracking()
                .Where(o => o.Id == request.Id)
                .ProjectToType<OffboardingEmployeeDetailsViewModel>()
                .FirstOrDefaultAsync(cancellationToken);

            if (onboarding == null)
            {
                return null;
            }

            var currentHodEmail = await UserHelper.GetLatestHODEmailByCompanyAsync(_context, onboarding.Branch, onboarding.Department);
            onboarding.HODEmail = string.IsNullOrEmpty(currentHodEmail)
                ? onboarding.HODEmail
                : currentHodEmail;

            return onboarding;
        }
    }
}
