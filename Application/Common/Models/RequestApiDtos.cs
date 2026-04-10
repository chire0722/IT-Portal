using IT_Form.Helpers;
using IT_Form.Models;
using Mapster;

namespace IT_Form.Application.Common.Models
{
    public sealed record ApiPageMetadata(
        int PageIndex,
        int TotalPages,
        int TotalCount,
        bool HasPreviousPage,
        bool HasNextPage);

    public sealed record AssetApiItem(
        string AssetType,
        string? Description,
        string? AssetTag,
        string? SerialNumber,
        string? BrandModel,
        int? Qty);

    public sealed record RequestedSoftwareApiItem(int? Id, string Name);

    public sealed record EmailGroupApiItem(int? Id, string Name, string? Email);

    public sealed record SharedMailboxApiItem(int? Id, string Name, string? Address);

    public sealed record FolderPermissionApiItem(string FolderName, bool CanRead, bool CanModify);

    public sealed record SignatureApiItem(string Role, string? SignerName, DateTime? SignedAt);

    public sealed record OnboardingListItemApiResponse(
        int Id,
        string? FullName,
        string? EmployeeId,
        string? Designation,
        string? Department,
        string? Branch,
        string Status,
        DateTime RequestDate,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? CompletedAt);

    public sealed record OnboardingListApiResponse(
        ApiPageMetadata Pagination,
        int SlaTargetDays,
        string? SearchString,
        string? StatusFilter,
        IReadOnlyCollection<OnboardingListItemApiResponse> Items);

    public sealed record OnboardingDetailsApiResponse(
        int Id,
        string? FullName,
        string? EmployeeId,
        string? Designation,
        string? Department,
        string? Branch,
        string? DeskNumber,
        DateTime? JoinDate,
        string? EmailAddress,
        string? ContactNumber,
        string? HodEmail,
        string Status,
        DateTime RequestDate,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? CompletedAt,
        int SlaTargetDays,
        DateTime? SlaStartTime,
        DateTime? SlaDeadline,
        string? IntegrityError,
        string? CompanyLogoPath,
        IReadOnlyCollection<AssetApiItem> IssuedAssets,
        IReadOnlyCollection<RequestedSoftwareApiItem> RequestedSoftware,
        IReadOnlyCollection<EmailGroupApiItem> EmailGroups,
        IReadOnlyCollection<SharedMailboxApiItem> SharedMailboxes,
        IReadOnlyCollection<FolderPermissionApiItem> FolderPermissions,
        IReadOnlyCollection<SignatureApiItem> Signatures);

    public sealed record OffboardingListItemApiResponse(
        int Id,
        string FullName,
        string? EmployeeId,
        string Department,
        string? Branch,
        string Status,
        DateTime LastDayOfWork,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? CompletedAt);

    public sealed record OffboardingListApiResponse(
        ApiPageMetadata Pagination,
        int SlaTargetDays,
        string? SearchString,
        string? StatusFilter,
        IReadOnlyCollection<OffboardingListItemApiResponse> Items);

    public sealed record OffboardingDetailsApiResponse(
        int Id,
        string FullName,
        string? EmployeeId,
        string Department,
        string? Branch,
        DateTime LastDayOfWork,
        string? EmailAddress,
        string HodEmail,
        string Status,
        DateTime RequestDate,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? CompletedAt,
        bool AccountDisabledAd,
        bool EmailForwarded,
        string? ForwardToEmail,
        bool M365LicenseRevoked,
        bool SoftwareLicensesRevoked,
        bool LaptopReturned,
        bool AccessoriesReturned,
        bool IsHardwareDamaged,
        string? HardwareNotes,
        decimal? MissingItemFines,
        string? FinesRemarks,
        bool DataBackupDone,
        bool EmailBackupDone,
        bool EmailForwardingDone,
        bool LicenseRemovalDone,
        string ManagerDisplayName,
        int SlaTargetDays,
        DateTime SlaStartTime,
        DateTime SlaDeadline,
        string? IntegrityError,
        string? CompanyLogoPath,
        IReadOnlyCollection<SignatureApiItem> Signatures);

    public sealed record AuditLogListItemApiResponse(
        int Id,
        string UserId,
        string PerformedBy,
        string Action,
        string EntityName,
        string EntityId,
        DateTime CreatedAt,
        bool IsSuccess,
        string? ErrorMessage,
        string? IpAddress,
        string? Details);

    public sealed record AuditLogListApiResponse(
        ApiPageMetadata Pagination,
        string? Entity,
        string? AuditAction,
        string? Search,
        DateTime? StartDate,
        DateTime? EndDate,
        bool? IsSuccess,
        IReadOnlyCollection<AuditLogListItemApiResponse> Items);

    public sealed record AuditLogDetailsApiResponse(
        int Id,
        string UserId,
        string PerformedBy,
        string Action,
        string EntityName,
        string EntityId,
        string SourceType,
        string StageLabel,
        DateTime CreatedAt,
        DateTime Timestamp,
        bool IsSuccess,
        string? ErrorMessage,
        string? IpAddress,
        string? Details,
        string? Payload,
        string? Changes,
        Guid? CorrelationId);

    public sealed record AuditLogBreakdownApiResponse(
        string Label,
        int Count);

    public sealed record AuditLogTrendPointApiResponse(
        DateTime Date,
        int Count);

    public sealed record AuditLogSummaryApiResponse(
        string? Entity,
        string? AuditAction,
        string? Search,
        DateTime? StartDate,
        DateTime? EndDate,
        bool? IsSuccess,
        int TotalCount,
        int SuccessCount,
        int FailureCount,
        int CommandCount,
        int FailedCommandCount,
        IReadOnlyCollection<AuditLogBreakdownApiResponse> TopFailedActions,
        IReadOnlyCollection<AuditLogBreakdownApiResponse> TopFailedActors,
        IReadOnlyCollection<AuditLogTrendPointApiResponse> FailureTrend);

    public sealed record AuditLogChainItemApiResponse(
        int Id,
        string UserId,
        string PerformedBy,
        string Action,
        string EntityName,
        string EntityId,
        string SourceType,
        string StageLabel,
        DateTime CreatedAt,
        bool IsSuccess,
        string? ErrorMessage,
        string? Details);

    public sealed record AuditLogChainApiResponse(
        int AnchorId,
        Guid? CorrelationId,
        bool HasCorrelation,
        bool FailuresOnly,
        int TotalCount,
        int FailureCount,
        IReadOnlyCollection<AuditLogChainItemApiResponse> Items);

    public static class RequestApiMappers
    {
        public static ApiPageMetadata ToApiPageMetadata<T>(this PaginatedList<T> page)
            => new(
                page.PageIndex,
                page.TotalPages,
                page.TotalCount,
                page.HasPreviousPage,
                page.HasNextPage);

        public static AssetApiItem ToApiItem(this IssuedAsset asset)
            => asset.Adapt<AssetApiItem>();

        public static RequestedSoftwareApiItem ToApiItem(this RequestSoftware software)
            => software.Adapt<RequestedSoftwareApiItem>();

        public static EmailGroupApiItem ToApiItem(this RequestEmailGroup group)
            => group.Adapt<EmailGroupApiItem>();

        public static SharedMailboxApiItem ToApiItem(this RequestSharedMailbox mailbox)
            => mailbox.Adapt<SharedMailboxApiItem>();

        public static FolderPermissionApiItem ToApiItem(this FolderPermission permission)
            => permission.Adapt<FolderPermissionApiItem>();
    }
}
