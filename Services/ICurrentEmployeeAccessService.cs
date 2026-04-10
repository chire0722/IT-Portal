using IT_Form.Models;
using IT_Form.ViewModels;

namespace IT_Form.Services
{
    public interface ICurrentEmployeeAccessService
    {
        Task<EmployeeContext?> GetEmployeeContextAsync(int onboardingId, CancellationToken cancellationToken = default);

        Task<AccessCatalogDto> GetAccessCatalogAsync(
            int? companyId,
            IEnumerable<int>? existingEmailGroupIds,
            IEnumerable<int>? existingSharedMailboxIds,
            IEnumerable<FolderPermission>? existingFolderPermissions,
            CancellationToken cancellationToken = default);

        List<FolderPermissionViewModel> BuildFolderPermissions(
            IEnumerable<string> availableFolderNames,
            IEnumerable<FolderPermission>? existingPermissions);

        void MapLegacyFlags(
            IEnumerable<RequestEmailGroup> emailGroups,
            IEnumerable<RequestSharedMailbox> sharedMailboxes,
            OnboardingRequest target);
    }

    public sealed class EmployeeContext
    {
        public OnboardingRequest Request { get; init; } = null!;
        public int? CompanyId { get; init; }
        public string? CurrentDepartment { get; init; }
        public string? CurrentBranch { get; init; }
    }

    public sealed class AccessCatalogDto
    {
        public IReadOnlyList<ManageAccessEmailGroupOptionViewModel> EmailGroups { get; init; } = [];
        public IReadOnlyList<ManageAccessSharedMailboxOptionViewModel> SharedMailboxes { get; init; } = [];
        public IReadOnlyList<string> ServerFolders { get; init; } = [];
    }
}
