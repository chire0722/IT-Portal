// Project comment: RequestApiDtos.cs is part of the IT Form application source code.

using IT_Form.Helpers;
using IT_Form.Models;
using Mapster;

namespace IT_Form.Application.Common.Models
{
    /// <summary>
    /// Defines the ApiPageMetadata record used by the IT Form application.
    /// </summary>
    public sealed record ApiPageMetadata(
        int PageIndex,
        int TotalPages,
        int TotalCount,
        bool HasPreviousPage,
        bool HasNextPage);

    /// <summary>
    /// Defines the AssetApiItem record used by the IT Form application.
    /// </summary>
    public sealed record AssetApiItem(
        string AssetType,
        string? Description,
        string? AssetTag,
        string? SerialNumber,
        string? BrandModel,
        int? Qty);

    /// <summary>
    /// Defines the RequestedSoftwareApiItem record used by the IT Form application.
    /// </summary>
    public sealed record RequestedSoftwareApiItem(int? Id, string Name);

    /// <summary>
    /// Defines the EmailGroupApiItem record used by the IT Form application.
    /// </summary>
    public sealed record EmailGroupApiItem(int? Id, string Name, string? Email);

    /// <summary>
    /// Defines the SharedMailboxApiItem record used by the IT Form application.
    /// </summary>
    public sealed record SharedMailboxApiItem(int? Id, string Name, string? Address);

    /// <summary>
    /// Defines the FolderPermissionApiItem record used by the IT Form application.
    /// </summary>
    public sealed record FolderPermissionApiItem(string FolderName, bool CanRead, bool CanModify);

    /// <summary>
    /// Defines the SignatureApiItem record used by the IT Form application.
    /// </summary>
    public sealed record SignatureApiItem(string Role, string? SignerName, DateTime? SignedAt);

    /// <summary>
    /// Defines the OnboardingListItemApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the OnboardingListApiResponse record used by the IT Form application.
    /// </summary>
    public sealed record OnboardingListApiResponse(
        ApiPageMetadata Pagination,
        int SlaTargetDays,
        string? SearchString,
        string? StatusFilter,
        IReadOnlyCollection<OnboardingListItemApiResponse> Items);

    /// <summary>
    /// Defines the OnboardingDetailsApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the OffboardingListItemApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the OffboardingListApiResponse record used by the IT Form application.
    /// </summary>
    public sealed record OffboardingListApiResponse(
        ApiPageMetadata Pagination,
        int SlaTargetDays,
        string? SearchString,
        string? StatusFilter,
        IReadOnlyCollection<OffboardingListItemApiResponse> Items);

    /// <summary>
    /// Defines the OffboardingDetailsApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the AuditLogListItemApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the AuditLogListApiResponse record used by the IT Form application.
    /// </summary>
    public sealed record AuditLogListApiResponse(
        ApiPageMetadata Pagination,
        string? Entity,
        string? AuditAction,
        string? Search,
        DateTime? StartDate,
        DateTime? EndDate,
        bool? IsSuccess,
        IReadOnlyCollection<AuditLogListItemApiResponse> Items);

    /// <summary>
    /// Defines the AuditLogDetailsApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the AuditLogBreakdownApiResponse record used by the IT Form application.
    /// </summary>
    public sealed record AuditLogBreakdownApiResponse(
        string Label,
        int Count);

    /// <summary>
    /// Defines the AuditLogTrendPointApiResponse record used by the IT Form application.
    /// </summary>
    public sealed record AuditLogTrendPointApiResponse(
        DateTime Date,
        int Count);

    /// <summary>
    /// Defines the AuditLogSummaryApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the AuditLogChainItemApiResponse record used by the IT Form application.
    /// </summary>
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

    /// <summary>
    /// Defines the AuditLogChainApiResponse record used by the IT Form application.
    /// </summary>
    public sealed record AuditLogChainApiResponse(
        int AnchorId,
        Guid? CorrelationId,
        bool HasCorrelation,
        bool FailuresOnly,
        int TotalCount,
        int FailureCount,
        IReadOnlyCollection<AuditLogChainItemApiResponse> Items);

    /// <summary>
    /// Defines the RequestApiMappers class used by the IT Form application.
    /// </summary>
    public static class RequestApiMappers
    {
        public static ApiPageMetadata ToApiPageMetadata<T>(this PaginatedList<T> page)
            => new(
                page.PageIndex,
                page.TotalPages,
                page.TotalCount,
                page.HasPreviousPage,
                page.HasNextPage);

        /// <summary>
        /// Runs the ToApiItem operation.
        /// </summary>
        public static AssetApiItem ToApiItem(this IssuedAsset asset)
            => asset.Adapt<AssetApiItem>();

        /// <summary>
        /// Runs the ToApiItem operation.
        /// </summary>
        public static RequestedSoftwareApiItem ToApiItem(this RequestSoftware software)
            => software.Adapt<RequestedSoftwareApiItem>();

        /// <summary>
        /// Runs the ToApiItem operation.
        /// </summary>
        public static EmailGroupApiItem ToApiItem(this RequestEmailGroup group)
            => group.Adapt<EmailGroupApiItem>();

        /// <summary>
        /// Runs the ToApiItem operation.
        /// </summary>
        public static SharedMailboxApiItem ToApiItem(this RequestSharedMailbox mailbox)
            => mailbox.Adapt<SharedMailboxApiItem>();

        /// <summary>
        /// Runs the ToApiItem operation.
        /// </summary>
        public static FolderPermissionApiItem ToApiItem(this FolderPermission permission)
            => permission.Adapt<FolderPermissionApiItem>();
    }
}
