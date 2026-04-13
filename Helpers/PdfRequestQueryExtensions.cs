using IT_Form.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Helpers
{
    public static class PdfRequestQueryExtensions
    {
        public static IQueryable<OnboardingRequest> IncludeOnboardingPdfData(this IQueryable<OnboardingRequest> query)
            => query
                .Include(request => request.IssuedAssets)
                .Include(request => request.FolderPermissions)
                .Include(request => request.RequestEmailGroups)
                .Include(request => request.RequestSharedMailboxes)
                .Include(request => request.RequestSoftware)
                .Include(request => request.IssuedSoftware)
                    .ThenInclude(issuedSoftware => issuedSoftware.SoftwareLicense);

        public static IQueryable<OffboardingRequest> IncludeOffboardingPdfData(this IQueryable<OffboardingRequest> query)
            => query
                .Include(request => request.OnboardingRequest)
                    .ThenInclude(onboarding => onboarding!.RequestEmailGroups)
                .Include(request => request.OnboardingRequest)
                    .ThenInclude(onboarding => onboarding!.RequestSharedMailboxes)
                .Include(request => request.OnboardingRequest)
                    .ThenInclude(onboarding => onboarding!.FolderPermissions)
                .Include(request => request.OnboardingRequest)
                    .ThenInclude(onboarding => onboarding!.RequestSoftware)
                .Include(request => request.OnboardingRequest)
                    .ThenInclude(onboarding => onboarding!.IssuedSoftware)
                        .ThenInclude(issuedSoftware => issuedSoftware.SoftwareLicense);
    }
}
