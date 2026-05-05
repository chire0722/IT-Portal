// Project comment: MapsterConfig.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Department;
using IT_Form.Application.Features.Account.Queries.GetProfilePage;
using IT_Form.Application.DTOs.Options;
using IT_Form.Application.Features.Company;
using DepartmentWriteCreateCommand = IT_Form.Application.Features.Departments.Writes.CreateDepartment.CreateDepartmentCommand;
using IT_Form.Application.Features.Department.GetDepartmentAssets;
using DepartmentWriteUpdateCommand = IT_Form.Application.Features.Departments.Writes.UpdateDepartment.UpdateDepartmentCommand;
using CompanyWriteCreateCommand = IT_Form.Application.Features.Company.Writes.CreateCompany.CreateCompanyCommand;
using CompanyWriteUpdateCommand = IT_Form.Application.Features.Company.Writes.UpdateCompany.UpdateCompanyCommand;
using IT_Form.Application.Features.EmailContact;
using IT_Form.Application.Features.EmailGroup;
using EmailGroupWriteCreateCommand = IT_Form.Application.Features.EmailGroups.Writes.CreateEmailGroup.CreateEmailGroupCommand;
using EmailGroupWriteUpdateCommand = IT_Form.Application.Features.EmailGroups.Writes.UpdateEmailGroup.UpdateEmailGroupCommand;
using EmailContactWriteCreateCommand = IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact.CreateEmailContactCommand;
using EmailContactWriteUpdateCommand = IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact.UpdateEmailContactCommand;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Application.Features.ServerFolder;
using ServerFolderWriteCreateCommand = IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder.CreateServerFolderCommand;
using ServerFolderWriteUpdateCommand = IT_Form.Application.Features.ServerFolders.Writes.UpdateServerFolder.UpdateServerFolderCommand;
using IT_Form.Application.Features.SharedMailbox;
using SharedMailboxWriteCreateCommand = IT_Form.Application.Features.SharedMailboxes.Writes.CreateSharedMailbox.CreateSharedMailboxCommand;
using SharedMailboxWriteUpdateCommand = IT_Form.Application.Features.SharedMailboxes.Writes.UpdateSharedMailbox.UpdateSharedMailboxCommand;
using IT_Form.Application.Features.StandardSoftware;
using StandardSoftwareWriteCreateCommand = IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware.CreateStandardSoftwareCommand;
using StandardSoftwareWriteUpdateCommand = IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware.UpdateStandardSoftwareCommand;
using IT_Form.Application.Features.UserManagement;
using UserManagementWriteCreateCommand = IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement.CreateUserManagementCommand;
using UserManagementWriteUpdateCommand = IT_Form.Application.Features.UserManagement.Writes.UpdateUserManagement.UpdateUserManagementCommand;
using AccountUpdateProfileCommand = IT_Form.Application.Features.Account.Writes.UpdateProfile.UpdateProfileCommand;
using AccountUpdateSignatureCommand = IT_Form.Application.Features.Account.Writes.UpdateSignature.UpdateSignatureCommand;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using Mapster;
using CompanyEntity = IT_Form.Models.Company;
using DepartmentEntity = IT_Form.Models.Department;
using EmailContactEntity = IT_Form.Models.EmailContact;
using EmailGroupEntity = IT_Form.Models.EmailGroup;
using ServerFolderEntity = IT_Form.Models.ServerFolder;
using SharedMailboxEntity = IT_Form.Models.SharedMailbox;
using StandardSoftwareEntity = IT_Form.Models.StandardSoftware;

namespace IT_Form.Application.Common.Mapping;

/// <summary>
/// Defines the MapsterConfig class used by the IT Form application.
/// </summary>
public static class MapsterConfig
{
    private static bool _isRegistered;

    /// <summary>
    /// Runs the RegisterMappings operation.
    /// </summary>
    public static void RegisterMappings()
    {
        if (_isRegistered)
        {
            return;
        }

        var config = TypeAdapterConfig.GlobalSettings;

        config.Default
            .NameMatchingStrategy(new NameMatchingStrategy
            {
                SourceMemberNameConverter = static memberName => memberName.ToLowerInvariant(),
                DestinationMemberNameConverter = static memberName => memberName.ToLowerInvariant()
            })
            .IgnoreNullValues(true);

        config.NewConfig<IssuedAsset, DepartmentAssetDto>()
            .Map(
                destination => destination.EmployeeName,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.FullName ?? source.CustomEmployeeName ?? string.Empty
                    : source.CustomEmployeeName ?? string.Empty)
            .Map(
                destination => destination.AssetName,
                source => source.ITAsset != null
                    ? source.ITAsset.Model ?? source.ITAsset.AssetTag
                    : source.BrandModel ?? source.Description ?? string.Empty)
            .Map(
                destination => destination.SerialNumber,
                source => source.ITAsset != null
                    ? source.ITAsset.SerialNumber ?? source.SerialNumber ?? string.Empty
                    : source.SerialNumber ?? string.Empty);

        config.NewConfig<ApplicationUser, UserManagementViewModel>()
            .Map(destination => destination.Username, source => source.UserName ?? string.Empty)
            .Map(destination => destination.IsIT, source => source.UserTitles.Any(title => title.Title == "IT"))
            .Map(destination => destination.IsHR, source => source.UserTitles.Any(title => title.Title == "HR"))
            .Map(destination => destination.IsHOD, source => source.UserTitles.Any(title => title.Title == "HOD"))
            .Map(destination => destination.IsSuperAdmin, source => source.UserTitles.Any(title => title.Title == "SuperAdmin"))
            .Map(destination => destination.SelectedCompanies, source => source.UserCompanies.Select(company => company.CompanyName).ToList());

        config.NewConfig<ApplicationUser, AccountProfilePageViewModel>()
            .Map(destination => destination.Titles, source => source.UserTitles.Select(title => title.Title).ToList());

        config.NewConfig<AccountUpdateProfileCommand, ApplicationUser>()
            .IgnoreNullValues(false)
            .Map(destination => destination.JobTitle,
                source => string.IsNullOrWhiteSpace(source.JobTitle) ? null : source.JobTitle.Trim())
            .Ignore(destination => destination.EmailSignatureImage!);

        config.NewConfig<AccountUpdateSignatureCommand, ApplicationUser>()
            .IgnoreNullValues(false)
            .Map(destination => destination.Signature,
                source => string.IsNullOrWhiteSpace(source.SignatureData) ? null : source.SignatureData);

        config.NewConfig<UserManagementEditorPageViewModel, UserManagementWriteCreateCommand>();

        config.NewConfig<UserManagementEditorPageViewModel, UserManagementWriteUpdateCommand>();

        config.NewConfig<UserManagementWriteCreateCommand, ApplicationUser>()
            .Map(destination => destination.UserName, source => source.Username)
            .Map(destination => destination.EmailConfirmed, _ => true)
            .Map(destination => destination.MustChangePassword, _ => true);

        config.NewConfig<UserManagementWriteUpdateCommand, ApplicationUser>()
            .Map(destination => destination.UserName, source => source.Username)
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.PasswordHash!)
            .Ignore(destination => destination.UserTitles)
            .Ignore(destination => destination.UserCompanies);

        config.NewConfig<CompanyEntity, CompanyIndexListItem>();

        config.NewConfig<CompanyEntity, CompanyEditorPageViewModel>();

        config.NewConfig<CompanyWriteCreateCommand, CompanyEntity>();

        config.NewConfig<CompanyWriteUpdateCommand, CompanyEntity>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.LogoPath!);

        config.NewConfig<Company, PageCompanyOption>()
            .Map(destination => destination.DisplayName,
                source => string.IsNullOrEmpty(source.Lot)
                    ? source.Name
                    : source.Name + " (" + source.Lot + ")");

        config.NewConfig<OnboardingRequest, PageCurrentEmployeeOption>()
            .Map(destination => destination.Label,
                source => (source.FullName ?? "Unknown") + " (" + (source.EmployeeID ?? "-") + ")");

        config.NewConfig<EmailContactEntity, EmailContactIndexListItem>();

        config.NewConfig<EmailContactEntity, EmailContactEditorPageViewModel>();

        config.NewConfig<EmailContactWriteCreateCommand, EmailContactEntity>();

        config.NewConfig<EmailContactWriteUpdateCommand, EmailContactEntity>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAt);

        config.NewConfig<DepartmentEntity, DepartmentIndexListItem>()
            .Map(destination => destination.Companies,
                source => source.DepartmentCompanies
                    .Select(company => company.Company.Name)
                    .ToList());

        config.NewConfig<DepartmentEntity, DepartmentEditorPageViewModel>()
            .Map(destination => destination.SelectedCompanyIds,
                source => source.DepartmentCompanies
                    .Select(company => company.CompanyId)
                    .ToList());

        config.NewConfig<DepartmentWriteCreateCommand, DepartmentEntity>()
            .Map(destination => destination.DepartmentCompanies,
                source => source.SelectedCompanyIds
                    .Distinct()
                    .Select(companyId => new DepartmentCompany
                    {
                        CompanyId = companyId
                    })
                    .ToList());

        config.NewConfig<DepartmentWriteUpdateCommand, DepartmentEntity>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.DepartmentCompanies);

        config.NewConfig<EmailGroupEntity, EmailGroupIndexListItem>()
            .Map(destination => destination.Companies,
                source => source.EmailGroupCompanies
                    .Select(company => company.Company.Name)
                    .ToList());

        config.NewConfig<EmailGroupEntity, EmailGroupEditorPageViewModel>()
            .Map(destination => destination.SelectedCompanyIds,
                source => source.EmailGroupCompanies
                    .Select(company => company.CompanyId)
                    .ToList());

        config.NewConfig<EmailGroupWriteCreateCommand, EmailGroupEntity>()
            .Map(destination => destination.EmailGroupCompanies,
                source => source.SelectedCompanyIds
                    .Distinct()
                    .Select(companyId => new EmailGroupCompany
                    {
                        CompanyId = companyId
                    })
                    .ToList());

        config.NewConfig<EmailGroupWriteUpdateCommand, EmailGroupEntity>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.EmailGroupCompanies);

        config.NewConfig<RequestEmailGroup, EmailGroupMemberListItem>()
            .Map(destination => destination.EmployeeName,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.FullName ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Department,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Department ?? "-"
                    : "-")
            .Map(destination => destination.RequestType, source => "Onboarding")
            .Map(destination => destination.RequestDate,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.RequestDate
                    : (DateTime?)null)
            .Map(destination => destination.Status,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Status
                    : (RequestStatus?)null)
            .Map(destination => destination.RequestId, source => source.OnboardingRequestId ?? 0)
            .Map(destination => destination.ControllerName, source => "Onboarding");

        config.NewConfig<RequestEmailGroup, EmailGroupAccessReportViewModel>()
            .Map(destination => destination.EmployeeName,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.FullName ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Department,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Department ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Branch,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Branch ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.GroupName,
                source => source.EmailGroup != null
                    ? source.EmailGroup.GroupName ?? source.GroupName ?? "Unknown"
                    : source.GroupName ?? "Unknown")
            .Map(destination => destination.GroupEmail,
                source => source.EmailGroup != null
                    ? source.EmailGroup.GroupEmail ?? source.GroupEmail ?? "Unknown"
                    : source.GroupEmail ?? "Unknown")
            .Map(destination => destination.RequestType, source => "Onboarding")
            .Map(destination => destination.RequestId, source => source.OnboardingRequestId ?? 0)
            .Map(destination => destination.DateGranted,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.CompletedAt
                    : (DateTime?)null);

        config.NewConfig<SharedMailboxEntity, SharedMailboxIndexListItem>()
            .Map(destination => destination.Companies,
                source => source.SharedMailboxCompanies
                    .Select(company => company.Company.Name)
                    .ToList());

        config.NewConfig<SharedMailboxEntity, SharedMailboxEditorPageViewModel>()
            .Map(destination => destination.SelectedCompanyIds,
                source => source.SharedMailboxCompanies
                    .Select(company => company.CompanyId)
                    .ToList());

        config.NewConfig<SharedMailboxWriteCreateCommand, SharedMailboxEntity>()
            .Map(destination => destination.SharedMailboxCompanies,
                source => source.SelectedCompanyIds
                    .Distinct()
                    .Select(companyId => new SharedMailboxCompany
                    {
                        CompanyId = companyId
                    })
                    .ToList());

        config.NewConfig<SharedMailboxWriteUpdateCommand, SharedMailboxEntity>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.SharedMailboxCompanies);

        config.NewConfig<RequestSharedMailbox, SharedMailboxMemberListItem>()
            .Map(destination => destination.EmployeeName,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.FullName ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Department,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Department ?? "-"
                    : "-")
            .Map(destination => destination.RequestType, source => "Onboarding")
            .Map(destination => destination.RequestDate,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.RequestDate
                    : (DateTime?)null)
            .Map(destination => destination.Status,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Status
                    : (RequestStatus?)null)
            .Map(destination => destination.RequestId, source => source.OnboardingRequestId ?? 0)
            .Map(destination => destination.ControllerName, source => "Onboarding");

        config.NewConfig<RequestSharedMailbox, SharedMailboxAccessReportViewModel>()
            .Map(destination => destination.EmployeeName,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.FullName ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Department,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Department ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Branch,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Branch ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.MailboxName,
                source => source.SharedMailbox != null
                    ? source.SharedMailbox.MailboxName ?? source.MailboxName ?? "Unknown"
                    : source.MailboxName ?? "Unknown")
            .Map(destination => destination.MailboxAddress,
                source => source.SharedMailbox != null
                    ? source.SharedMailbox.MailboxAddress ?? source.MailboxAddress ?? "Unknown"
                    : source.MailboxAddress ?? "Unknown")
            .Map(destination => destination.RequestType, source => "Onboarding")
            .Map(destination => destination.RequestId, source => source.OnboardingRequestId ?? 0)
            .Map(destination => destination.DateGranted,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.CompletedAt
                    : (DateTime?)null);

        config.NewConfig<ServerFolderEntity, ServerFolderIndexListItem>()
            .Map(destination => destination.Companies,
                source => source.ServerFolderCompanies
                    .Select(company => company.Company.Name)
                    .ToList());

        config.NewConfig<ServerFolderEntity, ServerFolderEditorPageViewModel>()
            .Map(destination => destination.SelectedCompanyIds,
                source => source.ServerFolderCompanies
                    .Select(company => company.CompanyId)
                    .ToList());

        config.NewConfig<ServerFolderWriteCreateCommand, ServerFolderEntity>()
            .Map(destination => destination.ServerFolderCompanies,
                source => source.SelectedCompanyIds
                    .Distinct()
                    .Select(companyId => new ServerFolderCompany
                    {
                        CompanyId = companyId
                    })
                    .ToList());

        config.NewConfig<ServerFolderWriteUpdateCommand, ServerFolderEntity>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.ServerFolderCompanies);

        config.NewConfig<FolderPermission, ServerFolderMemberListItem>()
            .Map(destination => destination.EmployeeName,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.FullName ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Department,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Department ?? "-"
                    : "-")
            .Map(destination => destination.RequestType, source => "Onboarding")
            .Map(destination => destination.RequestDate,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.RequestDate
                    : (DateTime?)null)
            .Map(destination => destination.Status,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Status
                    : (RequestStatus?)null)
            .Map(destination => destination.RequestId, source => source.OnboardingRequestId ?? 0)
            .Map(destination => destination.ControllerName, source => "Onboarding");

        config.NewConfig<FolderPermission, ServerFolderAccessReportViewModel>()
            .Map(destination => destination.EmployeeName,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.FullName ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Department,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Department ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.Branch,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.Branch ?? "Unknown"
                    : "Unknown")
            .Map(destination => destination.RequestType, source => "Onboarding")
            .Map(destination => destination.RequestId, source => source.OnboardingRequestId ?? 0)
            .Map(destination => destination.AccessLevel, source => source.CanModify ? "Read & Modify" : "Read Only")
            .Map(destination => destination.DateGranted,
                source => source.OnboardingRequest != null
                    ? source.OnboardingRequest.CompletedAt
                    : (DateTime?)null);

        config.NewConfig<OnboardingRequest, ManageAccessViewModel>()
            .Map(destination => destination.OnboardingId, source => source.Id)
            .Map(destination => destination.CurrentDepartment, source => source.Department)
            .Map(destination => destination.CurrentBranch, source => source.Branch)
            .Map(destination => destination.JoinedAt, source => source.JoinDate)
            .Map(destination => destination.SelectedEmailGroupIds,
                source => source.RequestEmailGroups
                    .Where(group => group.EmailGroupId > 0)
                    .Select(group => group.EmailGroupId)
                    .Distinct()
                    .ToList())
            .Map(destination => destination.SelectedSharedMailboxIds,
                source => source.RequestSharedMailboxes
                    .Where(mailbox => mailbox.SharedMailboxId > 0)
                    .Select(mailbox => mailbox.SharedMailboxId)
                    .Distinct()
                    .ToList());

        config.NewConfig<EmailGroup, ManageAccessEmailGroupOptionViewModel>();

        config.NewConfig<SharedMailbox, ManageAccessSharedMailboxOptionViewModel>();

        config.NewConfig<SoftwareLicense, SoftwareLicenseOptionDto>();

        config.NewConfig<StandardSoftwareEntity, StandardSoftwareOptionDto>()
            .Map(destination => destination.Licenses, source => source.Licenses);

        config.NewConfig<StandardSoftwareEntity, StandardSoftwareEditorPageViewModel>();

        config.NewConfig<StandardSoftwareWriteCreateCommand, StandardSoftwareEntity>();

        config.NewConfig<StandardSoftwareWriteUpdateCommand, StandardSoftwareEntity>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.Licenses)
            .Ignore(destination => destination.IsDeleted)
            .Ignore(destination => destination.DeletedAt!)
            .Ignore(destination => destination.DeletedByUserId!);

        config.NewConfig<StandardSoftwareEntity, StandardSoftwareIndexItem>()
            .Map(destination => destination.Licenses, source => source.Licenses);

        config.NewConfig<SoftwareLicense, StandardSoftwareLicenseListItem>()
            .Map(destination => destination.KeyCount, source => source.Keys.Count)
            .Map(destination => destination.CompanyIds,
                source => source.SoftwareLicenseCompanies
                    .Select(link => link.CompanyId)
                    .ToList())
            .Map(destination => destination.Companies,
                source => source.SoftwareLicenseCompanies
                    .Select(link => link.Company.Name)
                    .ToList());

        config.NewConfig<OnboardingRequest, StandardSoftwareEmployeeOptionDto>()
            .Map(destination => destination.Name, source => source.FullName ?? string.Empty);

        config.NewConfig<EmailGroup, EmailGroupOptionDto>();

        config.NewConfig<SharedMailbox, SharedMailboxOptionDto>();

        config.NewConfig<ITAsset, ITAssetOptionDto>();

        config.NewConfig<OnboardingRequest, OnboardingListItemApiResponse>()
            .Map(destination => destination.EmployeeId, source => source.EmployeeID)
            .Map(destination => destination.Status, source => source.Status.ToString());

        config.NewConfig<OnboardingRequest, OnboardingIndexListItem>()
            .Map(destination => destination.EmployeeId, source => source.EmployeeID);

        config.NewConfig<OnboardingIndexListItem, OnboardingListItemApiResponse>()
            .Map(destination => destination.Status, source => source.Status.ToString());

        config.NewConfig<OffboardingRequest, OffboardingListItemApiResponse>()
            .Map(destination => destination.EmployeeId, source => source.EmployeeID)
            .Map(destination => destination.Status, source => source.Status.ToString());

        config.NewConfig<OffboardingRequest, OffboardingIndexListItem>()
            .Map(destination => destination.EmployeeId, source => source.EmployeeID)
            .Map(destination => destination.HodEmail, source => source.HODEmail)
            .Map(destination => destination.AssignedHodName,
                source => source.AssignedHod != null
                    ? (!string.IsNullOrWhiteSpace(source.AssignedHod.FullName)
                        ? source.AssignedHod.FullName
                        : source.AssignedHod.Email)
                    : null);

        config.NewConfig<OffboardingIndexListItem, OffboardingListItemApiResponse>()
            .Map(destination => destination.EmployeeId, source => source.EmployeeId)
            .Map(destination => destination.Status, source => source.Status.ToString());

        config.NewConfig<IssuedAsset, RequestAssetReadModel>()
            .Map(destination => destination.OtherAssetType, source => source.OtherAssetType)
            .Map(destination => destination.AssetTag,
                source => !string.IsNullOrWhiteSpace(source.AssetTag)
                    ? source.AssetTag
                    : source.ITAsset != null
                        ? source.ITAsset.AssetTag
                        : null)
            .Map(destination => destination.BrandModel,
                source => !string.IsNullOrWhiteSpace(source.BrandModel)
                    ? source.BrandModel
                    : source.ITAsset != null
                        ? source.ITAsset.Model
                        : null)
            .Map(destination => destination.SerialNumber,
                source => !string.IsNullOrWhiteSpace(source.SerialNumber)
                    ? source.SerialNumber
                    : source.ITAsset != null
                        ? source.ITAsset.SerialNumber
                        : null)
            .Map(destination => destination.Description,
                source => !string.IsNullOrWhiteSpace(source.Description)
                    ? source.Description
                    : source.ITAsset != null
                        ? source.ITAsset.Model
                        : null)
            .Map(destination => destination.ITAsset, source => source.ITAsset)
            .Map(destination => destination.IsReturned,
                source => source.ITAsset != null && source.ITAsset.Status == AssetStatus.Available);

        config.NewConfig<ITAsset, RequestLinkedAssetReadModel>();

        config.NewConfig<IssuedSoftware, RequestIssuedSoftwareReadModel>()
            .Map(destination => destination.SoftwareName,
                source => source.SoftwareLicense != null
                    ? source.SoftwareLicense.SoftwareName
                    : source.SoftwareName ?? string.Empty);

        config.NewConfig<RequestSoftware, RequestSoftwareReadModel>()
            .Map(destination => destination.Id, source => source.StandardSoftwareId);

        config.NewConfig<RequestEmailGroup, RequestEmailGroupReadModel>()
            .Map(destination => destination.Id, source => source.EmailGroupId);

        config.NewConfig<RequestSharedMailbox, RequestSharedMailboxReadModel>()
            .Map(destination => destination.Id, source => source.SharedMailboxId);

        config.NewConfig<FolderPermission, RequestFolderPermissionReadModel>();

        config.NewConfig<OnboardingRequest, OnboardingDetailsPageViewModel>()
            .Map(destination => destination.IssuedAssets, source => source.IssuedAssets)
            .Map(destination => destination.IssuedSoftware, source => source.IssuedSoftware)
            .Map(destination => destination.RequestSoftware, source => source.RequestSoftware)
            .Map(destination => destination.RequestEmailGroups, source => source.RequestEmailGroups)
            .Map(destination => destination.RequestSharedMailboxes, source => source.RequestSharedMailboxes)
            .Map(destination => destination.FolderPermissions, source => source.FolderPermissions);

        config.NewConfig<OnboardingRequest, OffboardingOnboardingSnapshotViewModel>()
            .Map(destination => destination.IssuedAssets, source => source.IssuedAssets)
            .Map(destination => destination.IssuedSoftware, source => source.IssuedSoftware)
            .Map(destination => destination.FolderPermissions, source => source.FolderPermissions);

        config.NewConfig<OffboardingRequest, OffboardingDetailsPageViewModel>()
            .Map(destination => destination.OnboardingRequest, source => source.OnboardingRequest);

        config.NewConfig<OnboardingRequest, DashboardOnboardingQueueItem>();

        config.NewConfig<OffboardingRequest, DashboardOffboardingQueueItem>();

        config.NewConfig<OnboardingRequest, DashboardRequestSummaryItem>();

        config.NewConfig<OffboardingRequest, DashboardRequestSummaryItem>();

        config.NewConfig<SoftwareLicense, DashboardLicenseAlertItem>();

        config.NewConfig<ITAsset, DashboardAssetAlertItem>();

        config.NewConfig<AuditLog, DashboardAuditLogItem>();

        config.NewConfig<AuditLog, AuditLogListItemApiResponse>();

        config.NewConfig<AuditLog, AuditLogDetailsApiResponse>()
            .Map(destination => destination.SourceType, source => AuditLogClassifier.GetSourceType(source))
            .Map(destination => destination.StageLabel, source => AuditLogClassifier.GetStageLabel(source));

        config.NewConfig<AuditLog, AuditLogChainItemApiResponse>()
            .Map(destination => destination.SourceType, source => AuditLogClassifier.GetSourceType(source))
            .Map(destination => destination.StageLabel, source => AuditLogClassifier.GetStageLabel(source));

        config.NewConfig<IssuedAsset, AssetApiItem>()
            .Map(destination => destination.AssetType, source => AssetCategoryHelper.GetCategoryName(source.AssetType, source.OtherAssetType));

        config.NewConfig<RequestSoftware, RequestedSoftwareApiItem>()
            .Map(destination => destination.Id, source => source.StandardSoftwareId)
            .Map(destination => destination.Name, source => source.SoftwareName ?? string.Empty);

        config.NewConfig<RequestEmailGroup, EmailGroupApiItem>()
            .Map(destination => destination.Id, source => source.EmailGroupId)
            .Map(destination => destination.Name, source => source.GroupName ?? string.Empty)
            .Map(destination => destination.Email, source => source.GroupEmail);

        config.NewConfig<RequestSharedMailbox, SharedMailboxApiItem>()
            .Map(destination => destination.Id, source => source.SharedMailboxId)
            .Map(destination => destination.Name, source => source.MailboxName ?? string.Empty)
            .Map(destination => destination.Address, source => source.MailboxAddress);

        config.NewConfig<FolderPermission, FolderPermissionApiItem>();

        config.NewConfig<RequestAssetReadModel, AssetApiItem>()
            .Map(destination => destination.AssetType, source => AssetCategoryHelper.GetCategoryName(source.AssetType, source.OtherAssetType));

        config.NewConfig<RequestSoftwareReadModel, RequestedSoftwareApiItem>()
            .Map(destination => destination.Name, source => source.SoftwareName);

        config.NewConfig<RequestEmailGroupReadModel, EmailGroupApiItem>()
            .Map(destination => destination.Name, source => source.GroupName)
            .Map(destination => destination.Email, source => source.GroupEmail);

        config.NewConfig<RequestSharedMailboxReadModel, SharedMailboxApiItem>()
            .Map(destination => destination.Name, source => source.MailboxName)
            .Map(destination => destination.Address, source => source.MailboxAddress);

        config.NewConfig<RequestFolderPermissionReadModel, FolderPermissionApiItem>();

        config.NewConfig<GetOnboardingIndexResult, OnboardingListApiResponse>()
            .Map(destination => destination.Pagination, source => BuildPageMetadata(source.Requests))
            .Map(destination => destination.SearchString, source => source.CurrentFilter)
            .Map(destination => destination.StatusFilter, source => source.StatusFilter.HasValue ? source.StatusFilter.Value.ToString() : null)
            .Map(destination => destination.Items, source => source.Requests);

        config.NewConfig<OffboardingIndexPageViewModel, OffboardingListApiResponse>()
            .Map(destination => destination.Pagination, source => BuildPageMetadata(source.Requests))
            .Map(destination => destination.SearchString, source => source.CurrentFilter)
            .Map(destination => destination.StatusFilter, source => source.StatusFilter.HasValue ? source.StatusFilter.Value.ToString() : null)
            .Map(destination => destination.Items, source => source.Requests);

        config.NewConfig<OnboardingDetailsPageViewModel, OnboardingDetailsApiResponse>()
            .Map(destination => destination.EmployeeId, source => source.EmployeeID)
            .Map(destination => destination.HodEmail, source => source.HODEmail)
            .Map(destination => destination.Status, source => source.Status.ToString())
            .Map(destination => destination.IssuedAssets, source => source.IssuedAssets)
            .Map(destination => destination.RequestedSoftware, source => source.RequestSoftware)
            .Map(destination => destination.EmailGroups, source => source.RequestEmailGroups)
            .Map(destination => destination.SharedMailboxes, source => source.RequestSharedMailboxes)
            .Map(destination => destination.FolderPermissions, source => source.FolderPermissions)
            .Map(destination => destination.Signatures, source => BuildOnboardingSignatures(source));

        config.NewConfig<OffboardingDetailsPageViewModel, OffboardingDetailsApiResponse>()
            .Map(destination => destination.EmployeeId, source => source.EmployeeID)
            .Map(destination => destination.HodEmail, source => source.HODEmail)
            .Map(destination => destination.Status, source => source.Status.ToString())
            .Map(destination => destination.Signatures, source => BuildOffboardingSignatures(source));

        _isRegistered = true;
    }

    private static ApiPageMetadata BuildPageMetadata<T>(PaginatedList<T> page)
        => new(
            page.PageIndex,
            page.TotalPages,
            page.TotalCount,
            page.HasPreviousPage,
            page.HasNextPage);

    /// <summary>
    /// Builds the value returned by the BuildOnboardingSignatures operation.
    /// </summary>
    private static IReadOnlyCollection<SignatureApiItem> BuildOnboardingSignatures(OnboardingDetailsPageViewModel source)
        => new List<SignatureApiItem>
        {
            new("HR", source.RequesterSignerName, source.RequesterSignedAt),
            new("IT Admin", source.ITAdminSignerName, source.ITAdminSignedAt),
            new("Employee", source.EmployeeSignerName, source.EmployeeSignedAt)
        }
        .Where(signature => signature.SignedAt.HasValue || !string.IsNullOrWhiteSpace(signature.SignerName))
        .ToList();

    /// <summary>
    /// Builds the value returned by the BuildOffboardingSignatures operation.
    /// </summary>
    private static IReadOnlyCollection<SignatureApiItem> BuildOffboardingSignatures(OffboardingDetailsPageViewModel source)
        => new List<SignatureApiItem>
        {
            new("HR", source.HRSignerName, source.HRSignedAt),
            new("HOD", source.HODSignerName, source.HODSignedAt),
            new("IT Admin", source.ITAdminSignerName, source.ITAdminSignedAt),
            new("Employee", source.EmployeeSignerName, source.EmployeeSignedAt)
        }
        .Where(signature => signature.SignedAt.HasValue || !string.IsNullOrWhiteSpace(signature.SignerName))
        .ToList();
}
