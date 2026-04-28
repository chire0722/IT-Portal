// Project comment: RequestDetailsReadModels.cs is part of the IT Form application source code.

using IT_Form.Models.Enums;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Defines the RequestAssetReadModel class used by the IT Form application.
    /// </summary>
    public sealed class RequestAssetReadModel
    {
        public AssetType AssetType { get; set; }
        public string? OtherAssetType { get; set; }
        public string? Description { get; set; }
        public string? AssetTag { get; set; }
        public string? SerialNumber { get; set; }
        public string? BrandModel { get; set; }
        public int? Qty { get; set; }
        public bool IsReturned { get; set; }
        public RequestLinkedAssetReadModel? ITAsset { get; set; }
    }

    /// <summary>
    /// Defines the RequestLinkedAssetReadModel class used by the IT Form application.
    /// </summary>
    public sealed class RequestLinkedAssetReadModel
    {
        public string AssetTag { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? Model { get; set; }
        public AssetStatus Status { get; set; }
    }

    /// <summary>
    /// Defines the RequestIssuedSoftwareReadModel class used by the IT Form application.
    /// </summary>
    public sealed class RequestIssuedSoftwareReadModel
    {
        public string SoftwareName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Defines the RequestSoftwareReadModel class used by the IT Form application.
    /// </summary>
    public sealed class RequestSoftwareReadModel
    {
        public int? Id { get; set; }
        public string SoftwareName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Defines the RequestEmailGroupReadModel class used by the IT Form application.
    /// </summary>
    public sealed class RequestEmailGroupReadModel
    {
        public int? Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupEmail { get; set; }
    }

    /// <summary>
    /// Defines the RequestSharedMailboxReadModel class used by the IT Form application.
    /// </summary>
    public sealed class RequestSharedMailboxReadModel
    {
        public int? Id { get; set; }
        public string MailboxName { get; set; } = string.Empty;
        public string? MailboxAddress { get; set; }
    }

    /// <summary>
    /// Defines the RequestFolderPermissionReadModel class used by the IT Form application.
    /// </summary>
    public sealed class RequestFolderPermissionReadModel
    {
        public string FolderName { get; set; } = string.Empty;
        public bool CanRead { get; set; }
        public bool CanModify { get; set; }
    }
}
