// Project comment: OnboardingFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2;
using IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest;
using IT_Form.Application.Features.Onboarding.EditOnboardingRequest;
using IT_Form.Application.Features.Onboarding.GetOnboardingStep1Options;
using IT_Form.Application.Features.Onboarding.RejectOnboardingRequest;
using IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken;
using IT_Form.Application.Features.Onboarding.SaveOnboardingDraft;
using IT_Form.Application.Features.Onboarding.SaveOnboardingStep1;
using IT_Form.Application.Features.Onboarding.SignOffHrOnboarding;
using IT_Form.Application.Features.Onboarding.SignOnboardingRequest;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4;
using IT_Form.Application.Features.Onboarding.UpdateManageAccess;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the OnboardingFeatureSliceTests class used by the IT Form application.
/// </summary>
public class OnboardingFeatureSliceTests
{
    [Fact]
    /// <summary>
    /// Runs the SignValidator_Fails_WhenSignatureDataMissing operation.
    /// </summary>
    public void SignValidator_Fails_WhenSignatureDataMissing()
    {
        var validator = new SignOnboardingRequestCommandValidator();

        var result = validator.Validate(new SignOnboardingRequestCommand(1, "ITAdmin", string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the SignHandler_ReturnsForbiddenRole_WhenUserIsNotIT operation.
    /// </summary>
    public async Task SignHandler_ReturnsForbiddenRole_WhenUserIsNotIT()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 101,
            Status = RequestStatus.PendingITApproval,
            CreatedByUserId = "creator",
            Branch = "HQ"
        });
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("hr-user", "HR", "HQ"));
        currentUser.SetupGet(x => x.UserName).Returns("hr-user");

        var userManager = CreateUserManager();
        var integrity = new Mock<ISignatureIntegrityService>();
        var logger = Mock.Of<ILogger<SignOnboardingRequestCommandHandler>>();

        var handler = new SignOnboardingRequestCommandHandler(
            context,
            currentUser.Object,
            userManager.Object,
            integrity.Object,
            logger);

        var result = await handler.Handle(
            new SignOnboardingRequestCommand(101, "ITAdmin", "base64-signature"),
            CancellationToken.None);

        Assert.Equal(SignOnboardingRequestStatus.ForbiddenRole, result.Status);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateChecklistValidator_Fails_WhenRemarksTooLong operation.
    /// </summary>
    public void UpdateChecklistValidator_Fails_WhenRemarksTooLong()
    {
        var validator = new UpdateOnboardingChecklistCommandValidator();
        var longRemarks = new string('x', 2001);

        var result = validator.Validate(new UpdateOnboardingChecklistCommand(
            1, false, false, false, false, false, false, false, false, false, false, false, false, false, false, longRemarks));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateChecklistHandler_UpdatesChecklist_WhenRequestIsEditable operation.
    /// </summary>
    public async Task UpdateChecklistHandler_UpdatesChecklist_WhenRequestIsEditable()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 201,
            Status = RequestStatus.PendingITApproval,
            CheckO365Setup = false
        });
        await context.SaveChangesAsync();

        var handler = new UpdateOnboardingChecklistCommandHandler(context);

        var result = await handler.Handle(new UpdateOnboardingChecklistCommand(
            201,
            CheckPhysicalClean: true,
            CheckO365Setup: true,
            CheckWindowsLogin: false,
            CheckEmailSetup: false,
            CheckSpecificSoftware: false,
            CheckGeneralSoftware: false,
            CheckAbcGroup: false,
            CheckEmailMailGroup: false,
            CheckDomainJoin: false,
            CheckSharedDrive: false,
            CheckWiFiSetup: false,
            CheckPrinterSetup: false,
            CheckScanFolder: false,
            CheckVPNSetup: false,
            ChecklistRemarks: "updated"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("IT Checklist updated successfully.", result.Message);
        Assert.Equal(201, result.RequestId);

        var saved = await context.OnboardingRequests.FindAsync(201);
        Assert.NotNull(saved);
        Assert.True(saved!.CheckPhysicalClean);
        Assert.True(saved.CheckO365Setup);
        Assert.Equal("updated", saved.ChecklistRemarks);
    }

    [Fact]
    /// <summary>
    /// Runs the RegenerateTokenValidator_Fails_WhenIdInvalid operation.
    /// </summary>
    public void RegenerateTokenValidator_Fails_WhenIdInvalid()
    {
        var validator = new RegenerateOnboardingTokenCommandValidator();

        var result = validator.Validate(new RegenerateOnboardingTokenCommand(0));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the RegenerateTokenHandler_Regenerates_WhenPendingEmployeeSign operation.
    /// </summary>
    public async Task RegenerateTokenHandler_Regenerates_WhenPendingEmployeeSign()
    {
        using var context = CreateContext();
        var oldToken = Guid.NewGuid();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 301,
            Status = RequestStatus.PendingEmployeeSign,
            AccessToken = oldToken,
            Branch = "HQ",
            CreatedByUserId = "creator"
        });
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("hr-user", "HR", "HQ"));
        currentUser.SetupGet(x => x.UserName).Returns("hr-user");

        var handler = new RegenerateOnboardingTokenCommandHandler(context, currentUser.Object);

        var result = await handler.Handle(new RegenerateOnboardingTokenCommand(301), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Signature link has been regenerated. The old link is now invalid.", result.Message);
        Assert.Equal(301, result.RequestId);

        var saved = await context.OnboardingRequests.FindAsync(301);
        Assert.NotNull(saved);
        Assert.NotEqual(oldToken, saved!.AccessToken);
    }

    [Fact]
    /// <summary>
    /// Runs the RegenerateTokenHandler_RejectsNonHrDirectInvocation operation.
    /// </summary>
    public async Task RegenerateTokenHandler_RejectsNonHrDirectInvocation()
    {
        using var context = CreateContext();
        var oldToken = Guid.NewGuid();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 302,
            Status = RequestStatus.PendingEmployeeSign,
            AccessToken = oldToken,
            Branch = "HQ",
            CreatedByUserId = "manager-user"
        });
        await context.SaveChangesAsync();

        var handler = new RegenerateOnboardingTokenCommandHandler(
            context,
            new TestCurrentUserAccessor("manager-user", titles: ["Manager"], companies: ["HQ"]));

        var result = await handler.Handle(new RegenerateOnboardingTokenCommand(302), CancellationToken.None);

        Assert.True(result.IsForbidden);
        var saved = await context.OnboardingRequests.FindAsync(302);
        Assert.NotNull(saved);
        Assert.Equal(oldToken, saved!.AccessToken);
    }

    [Fact]
    /// <summary>
    /// Runs the RegenerateTokenHandler_HidesMissingRecordFromNonHr operation.
    /// </summary>
    public async Task RegenerateTokenHandler_HidesMissingRecordFromNonHr()
    {
        using var context = CreateContext();
        var handler = new RegenerateOnboardingTokenCommandHandler(
            context,
            new TestCurrentUserAccessor("manager-user", titles: ["Manager"], companies: ["HQ"]));

        var result = await handler.Handle(new RegenerateOnboardingTokenCommand(999), CancellationToken.None);

        Assert.True(result.IsForbidden);
        Assert.False(result.IsNotFound);
    }

    [Fact]
    /// <summary>
    /// Runs the RejectValidator_Fails_WhenReasonMissing operation.
    /// </summary>
    public void RejectValidator_Fails_WhenReasonMissing()
    {
        var validator = new RejectOnboardingRequestCommandValidator();

        var result = validator.Validate(new RejectOnboardingRequestCommand(1, string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the RejectHandler_ReturnsRejected_WhenUserIsITAndRequestEditable operation.
    /// </summary>
    public async Task RejectHandler_ReturnsRejected_WhenUserIsITAndRequestEditable()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 401,
            Status = RequestStatus.PendingITApproval,
            CreatedByUserId = "creator",
            Branch = "HQ"
        });
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("it-user", "IT", "HQ"));
        currentUser.SetupGet(x => x.UserName).Returns("it-user");

        var logger = Mock.Of<ILogger<RejectOnboardingRequestCommandHandler>>();
        var handler = new RejectOnboardingRequestCommandHandler(context, currentUser.Object, logger);

        var result = await handler.Handle(new RejectOnboardingRequestCommand(401, "Invalid request"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Request has been rejected.", result.Message);
        Assert.Equal(401, result.RequestId);

        var saved = await context.OnboardingRequests.FindAsync(401);
        Assert.NotNull(saved);
        Assert.Equal(RequestStatus.Rejected, saved!.Status);
        Assert.Equal("Invalid request", saved.RejectionReason);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateManageAccessValidator_Fails_WhenIdInvalid operation.
    /// </summary>
    public void UpdateManageAccessValidator_Fails_WhenIdInvalid()
    {
        var validator = new UpdateManageAccessCommandValidator();

        var result = validator.Validate(new UpdateManageAccessCommand(
            0,
            Array.Empty<int>(),
            null,
            Array.Empty<int>(),
            null,
            Array.Empty<FolderPermissionViewModel>(),
            null));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateManageAccessHandler_UpdatesAssignments_WhenRequestIsCurrentEmployee operation.
    /// </summary>
    public async Task UpdateManageAccessHandler_UpdatesAssignments_WhenRequestIsCurrentEmployee()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 501,
            FullName = "Current Employee",
            Branch = "HQ",
            Status = RequestStatus.Completed
        });
        context.EmailGroups.Add(new EmailGroup { Id = 1, GroupName = "All Staff", GroupEmail = "all@example.com", IsActive = true });
        context.SharedMailboxes.Add(new SharedMailbox { Id = 2, MailboxName = "Admin", MailboxAddress = "admin@example.com", IsActive = true });
        context.ServerFolders.Add(new ServerFolder { Id = 3, FolderName = "Common", IsActive = true });
        await context.SaveChangesAsync();

        var handler = new UpdateManageAccessCommandHandler(context, new CurrentEmployeeAccessService(context));

        var result = await handler.Handle(new UpdateManageAccessCommand(
            501,
            new[] { 1 },
            "Other Group",
            new[] { 2 },
            "Other Mailbox",
            new[]
            {
                new FolderPermissionViewModel
                {
                    FolderName = "Common",
                    CanRead = true,
                    CanModify = false
                }
            },
            "Other Folder"),
            CancellationToken.None);

        Assert.Equal(UpdateManageAccessStatus.Updated, result.Status);

        var saved = await context.OnboardingRequests
            .Include(r => r.RequestEmailGroups)
            .Include(r => r.RequestSharedMailboxes)
            .Include(r => r.FolderPermissions)
            .FirstAsync(r => r.Id == 501);

        Assert.Single(saved.RequestEmailGroups);
        Assert.Single(saved.RequestSharedMailboxes);
        Assert.Single(saved.FolderPermissions);
        Assert.True(saved.OtherGroup);
        Assert.True(saved.HasOtherSharedMailbox);
        Assert.True(saved.HasOtherFolder);
    }

    [Fact]
    /// <summary>
    /// Runs the SignOffHrValidator_Fails_WhenSignatureMissing operation.
    /// </summary>
    public void SignOffHrValidator_Fails_WhenSignatureMissing()
    {
        var validator = new SignOffHrOnboardingCommandValidator();

        var result = validator.Validate(new SignOffHrOnboardingCommand(1, string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the SignOffHrHandler_SetsDraftToPendingITApproval_WhenValid operation.
    /// </summary>
    public async Task SignOffHrHandler_SetsDraftToPendingITApproval_WhenValid()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 601,
            FullName = "Draft Employee",
            Status = RequestStatus.Draft
        });
        await context.SaveChangesAsync();

        var integrity = new Mock<ISignatureIntegrityService>();
        integrity.Setup(x => x.ComputeHmac(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns("HMAC");
        var logger = Mock.Of<ILogger<SignOffHrOnboardingCommandHandler>>();
        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.UserName).Returns("hr-user");
        var handler = new SignOffHrOnboardingCommandHandler(context, integrity.Object, logger, currentUser.Object);

        var result = await handler.Handle(new SignOffHrOnboardingCommand(601, "base64-signature"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Signature saved! The request has been submitted to IT.", result.Message);
        Assert.Equal(601, result.RequestId);

        var saved = await context.OnboardingRequests.FindAsync(601);
        Assert.NotNull(saved);
        Assert.Equal(RequestStatus.PendingITApproval, saved!.Status);
        Assert.Equal("base64-signature", saved.RequesterSignature);
        Assert.Equal("HMAC", saved.RequesterSignatureHmac);
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteOnboardingRequestValidator_Fails_WhenIdInvalid operation.
    /// </summary>
    public void DeleteOnboardingRequestValidator_Fails_WhenIdInvalid()
    {
        var validator = new DeleteOnboardingRequestCommandValidator();

        var result = validator.Validate(new DeleteOnboardingRequestCommand(0, false));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteOnboardingRequestHandler_DeletesDraft_WhenAllowed operation.
    /// </summary>
    public async Task DeleteOnboardingRequestHandler_DeletesDraft_WhenAllowed()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 701,
            FullName = "Delete Me",
            Status = RequestStatus.Draft,
            Branch = "HQ",
            CreatedByUserId = "creator"
        });
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("it-user", "IT", "HQ"));
        currentUser.SetupGet(x => x.UserName).Returns("it-user");

        var logger = Mock.Of<ILogger<DeleteOnboardingRequestCommandHandler>>();
        var handler = new DeleteOnboardingRequestCommandHandler(context, currentUser.Object, logger);

        var result = await handler.Handle(new DeleteOnboardingRequestCommand(701, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("has been deleted", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await context.OnboardingRequests.FindAsync(701));
    }

    [Fact]
    /// <summary>
    /// Saves data for the SaveOnboardingDraftValidator_Fails_WhenModelMissing operation.
    /// </summary>
    public void SaveOnboardingDraftValidator_Fails_WhenModelMissing()
    {
        var validator = new SaveOnboardingDraftCommandValidator();

        var result = validator.Validate(new SaveOnboardingDraftCommand(null!, "hr-user"));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Saves data for the SaveOnboardingDraftHandler_SavesDraft_WhenEditable operation.
    /// </summary>
    public async Task SaveOnboardingDraftHandler_SavesDraft_WhenEditable()
    {
        using var context = CreateContext();
        var wizardService = new OnboardingWizardService(context);
        var handler = new SaveOnboardingDraftCommandHandler(
            context,
            wizardService,
            new TestCurrentUserAccessor("hr-user", companies: ["HQ"]));

        var model = new OnboardingViewModel
        {
            FullName = "Draft User",
            EmployeeID = "EMP001",
            Designation = "Engineer",
            Department = "IT",
            Branch = "HQ",
            HODEmail = "hod@example.com"
        };

        var result = await handler.Handle(new SaveOnboardingDraftCommand(model, "hr-user"), CancellationToken.None);

        Assert.Equal(SaveOnboardingDraftStatus.Saved, result.Status);

        var saved = await context.OnboardingRequests.FindAsync(result.RequestId);
        Assert.NotNull(saved);
        Assert.Equal(RequestStatus.Draft, saved!.Status);
        Assert.Equal("Draft User", saved.FullName);
    }

    [Fact]
    /// <summary>
    /// Runs the CompleteOnboardingItSetupValidator_Fails_WhenSessionIdInvalid operation.
    /// </summary>
    public void CompleteOnboardingItSetupValidator_Fails_WhenSessionIdInvalid()
    {
        var validator = new CompleteOnboardingItSetupCommandValidator();

        var result = validator.Validate(new CompleteOnboardingItSetupCommand
        {
            Session = new OnboardingViewModel(),
            IsDraft = true
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the CompleteOnboardingItSetupHandler_SavesDraft_WhenRequested operation.
    /// </summary>
    public async Task CompleteOnboardingItSetupHandler_SavesDraft_WhenRequested()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 801,
            FullName = "IT Draft",
            Branch = "HQ",
            Status = RequestStatus.PendingITApproval,
            FolderPermissions = new List<FolderPermission>(),
            RequestSoftware = new List<RequestSoftware>(),
            RequestEmailGroups = new List<RequestEmailGroup>(),
            RequestSharedMailboxes = new List<RequestSharedMailbox>(),
            IssuedAssets = new List<IssuedAsset>()
        });
        await context.SaveChangesAsync();

        var logger = Mock.Of<ILogger<CompleteOnboardingItSetupCommandHandler>>();
        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("it-user", "IT", "HQ"));
        var userManager = CreateUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Signature = "it-signature" });
        var integrity = new Mock<ISignatureIntegrityService>();
        integrity.Setup(x => x.ComputeHmac(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns("HMAC");
        var handler = new CompleteOnboardingItSetupCommandHandler(context, logger, userManager.Object, currentUser.Object, integrity.Object);

        var session = new OnboardingViewModel { Id = 801 };
        var result = await handler.Handle(new CompleteOnboardingItSetupCommand
        {
            Session = session,
            IsDraft = true,
            CheckPhysicalClean = true,
            CheckO365Setup = true,
            CheckWindowsLogin = true,
            CheckEmailSetup = true,
            CheckSpecificSoftware = true,
            CheckGeneralSoftware = true,
            CheckAbcGroup = true,
            CheckEmailMailGroup = true,
            CheckDomainJoin = true,
            CheckSharedDrive = true,
            CheckWiFiSetup = true,
            CheckPrinterSetup = true,
            CheckScanFolder = true,
            CheckVPNSetup = true,
            ChecklistRemarks = "draft remarks"
        },
            CancellationToken.None);

        Assert.Equal(CompleteOnboardingItSetupStatus.DraftSaved, result.Status);

        var saved = await context.OnboardingRequests.FindAsync(801);
        Assert.NotNull(saved);
        Assert.Equal("draft remarks", saved!.ChecklistRemarks);
        Assert.True(saved.CheckPhysicalClean);
    }

    [Fact]
    /// <summary>
    /// Runs the CompleteOnboardingItSetupHandler_RejectsAssetFromDifferentCompany operation.
    /// </summary>
    public async Task CompleteOnboardingItSetupHandler_RejectsAssetFromDifferentCompany()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 802,
            FullName = "Cross Company User",
            Branch = "HQ",
            Status = RequestStatus.PendingITApproval,
            FolderPermissions = new List<FolderPermission>(),
            RequestSoftware = new List<RequestSoftware>(),
            RequestEmailGroups = new List<RequestEmailGroup>(),
            RequestSharedMailboxes = new List<RequestSharedMailbox>(),
            IssuedAssets = new List<IssuedAsset>()
        });
        context.ITAssets.Add(new ITAsset
        {
            Id = 1,
            AssetTag = "OTHER-001",
            SerialNumber = "OTHER-SN-001",
            AssetType = AssetType.Laptop,
            Status = AssetStatus.Available,
            Company = "Other Company"
        });
        await context.SaveChangesAsync();

        var logger = Mock.Of<ILogger<CompleteOnboardingItSetupCommandHandler>>();
        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("it-user", "IT", "HQ"));
        var userManager = CreateUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Signature = "it-signature" });
        var integrity = new Mock<ISignatureIntegrityService>();
        integrity.Setup(x => x.ComputeHmac(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns("HMAC");
        var handler = new CompleteOnboardingItSetupCommandHandler(context, logger, userManager.Object, currentUser.Object, integrity.Object);

        var result = await handler.Handle(new CompleteOnboardingItSetupCommand
        {
            Session = new OnboardingViewModel
            {
                Id = 802,
                IssuedAssets = new List<AssetItemViewModel>
                {
                    new() { ITAssetId = 1, AssetType = AssetType.Laptop, IsChecked = true }
                }
            },
            IsDraft = false,
            CheckPhysicalClean = true,
            CheckO365Setup = true,
            CheckWindowsLogin = true,
            CheckEmailSetup = true,
            CheckSpecificSoftware = true,
            CheckGeneralSoftware = true,
            CheckAbcGroup = true,
            CheckEmailMailGroup = true,
            CheckDomainJoin = true,
            CheckSharedDrive = true,
            CheckWiFiSetup = true,
            CheckPrinterSetup = true,
            CheckScanFolder = true,
            CheckVPNSetup = true
        }, CancellationToken.None);

        Assert.Equal(CompleteOnboardingItSetupStatus.InvalidAssetSelection, result.Status);
        Assert.Equal(AssetStatus.Available, (await context.ITAssets.FindAsync(1))!.Status);
        Assert.Equal(RequestStatus.PendingITApproval, (await context.OnboardingRequests.FindAsync(802))!.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the EditOnboardingRequestValidator_Fails_WhenIdInvalid operation.
    /// </summary>
    public async Task EditOnboardingRequestValidator_Fails_WhenIdInvalid()
    {
        var hodService = new Mock<IOnboardingHodService>();
        hodService.Setup(x => x.IsValidHodForCompanyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var validator = new EditOnboardingRequestCommandValidator(hodService.Object);

        var result = await validator.ValidateAsync(new EditOnboardingRequestCommand(
            0,
            new OnboardingRequest(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            "save"));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the EditOnboardingRequestHandler_UpdatesRequest_WhenEditable operation.
    /// </summary>
    public async Task EditOnboardingRequestHandler_UpdatesRequest_WhenEditable()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 1, SoftwareName = "Office", IsActive = true });
        context.EmailGroups.Add(new EmailGroup { Id = 2, GroupName = "All Staff", IsActive = true });
        context.SharedMailboxes.Add(new SharedMailbox { Id = 3, MailboxName = "IT", IsActive = true });
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 901,
            FullName = "Editable",
            Status = RequestStatus.Draft,
            RequestSoftware = new List<RequestSoftware>(),
            RequestEmailGroups = new List<RequestEmailGroup>(),
            RequestSharedMailboxes = new List<RequestSharedMailbox>(),
            IssuedAssets = new List<IssuedAsset>(),
            FolderPermissions = new List<FolderPermission>()
        });
        await context.SaveChangesAsync();

        var logger = Mock.Of<ILogger<EditOnboardingRequestCommandHandler>>();
        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("hr-user", "HR", "HQ"));
        currentUser.SetupGet(x => x.UserName).Returns("hr-user");
        var userManager = CreateUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { FullName = "HR User", Signature = "base64-signature" });
        var handler = new EditOnboardingRequestCommandHandler(
            context,
            Mock.Of<ISignatureIntegrityService>(),
            logger,
            userManager.Object,
            currentUser.Object);

        var model = new OnboardingRequest
        {
            FullName = "Edited Name",
            EmployeeID = "EMP9",
            Designation = "Engineer",
            Department = "IT",
            Branch = "HQ",
            HODEmail = "hod@example.com",
            IssuedAssets = new List<IssuedAsset>()
        };

        var result = await handler.Handle(new EditOnboardingRequestCommand(
            901,
            model,
            new[] { 1 },
            new[] { 2 },
            new[] { 3 },
            "save"),
            CancellationToken.None);

        Assert.Equal(EditOnboardingRequestStatus.Updated, result.Status);

        var saved = await context.OnboardingRequests
            .Include(r => r.RequestSoftware)
            .Include(r => r.RequestEmailGroups)
            .Include(r => r.RequestSharedMailboxes)
            .FirstAsync(r => r.Id == 901);
        Assert.Equal("Edited Name", saved.FullName);
        Assert.Single(saved.RequestSoftware);
        Assert.Single(saved.RequestEmailGroups);
        Assert.Single(saved.RequestSharedMailboxes);
    }

    [Fact]
    /// <summary>
    /// Saves data for the SaveOnboardingStep1Validator_Fails_WhenModelMissing operation.
    /// </summary>
    public async Task SaveOnboardingStep1Validator_Fails_WhenModelMissing()
    {
        var hodService = new Mock<IOnboardingHodService>();
        hodService.Setup(x => x.IsValidHodForCompanyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var validator = new SaveOnboardingStep1CommandValidator(hodService.Object);

        var result = await validator.ValidateAsync(new SaveOnboardingStep1Command(null!, new OnboardingViewModel()));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Saves data for the SaveOnboardingStep1Handler_SavesDraftAndAdvancesStep operation.
    /// </summary>
    public async Task SaveOnboardingStep1Handler_SavesDraftAndAdvancesStep()
    {
        using var context = CreateContext();
        var wizard = new Mock<IOnboardingWizardService>();
        wizard.Setup(x => x.MapStep1ToSession(It.IsAny<OnboardingViewModel>(), It.IsAny<OnboardingViewModel>()))
            .Callback((OnboardingViewModel source, OnboardingViewModel target) =>
            {
                target.FullName = source.FullName;
                target.EmployeeID = source.EmployeeID;
                target.Designation = source.Designation;
                target.Department = source.Department;
                target.Branch = source.Branch;
                target.DeskNumber = source.DeskNumber;
                target.JoinDate = source.JoinDate;
                target.EmailAddress = source.EmailAddress;
                target.ContactNumber = source.ContactNumber;
                target.HODEmail = source.HODEmail;
            });
        wizard.Setup(x => x.MapSessionToRequest(It.IsAny<OnboardingViewModel>(), It.IsAny<OnboardingRequest>()))
            .Callback((OnboardingViewModel source, OnboardingRequest target) =>
            {
                target.FullName = source.FullName;
                target.EmployeeID = source.EmployeeID;
                target.Designation = source.Designation;
                target.Department = source.Department;
                target.Branch = source.Branch;
                target.DeskNumber = source.DeskNumber;
                target.JoinDate = source.JoinDate;
                target.EmailAddress = source.EmailAddress;
                target.ContactNumber = source.ContactNumber;
                target.HODEmail = source.HODEmail;
            });

        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.UserName).Returns("hr-user");
        var handler = new SaveOnboardingStep1CommandHandler(context, wizard.Object, currentUser.Object);
        var session = new OnboardingViewModel();
        var model = new OnboardingViewModel
        {
            FullName = "New Hire",
            EmployeeID = "EMP100",
            Designation = "Engineer",
            Department = "IT",
            Branch = "HQ",
            DeskNumber = "A-1",
            JoinDate = DateTime.Today,
            EmailAddress = "new.hire@example.com",
            ContactNumber = "0123456789",
            HODEmail = "hod@example.com"
        };

        var result = await handler.Handle(new SaveOnboardingStep1Command(model, session), CancellationToken.None);

        Assert.Equal(SaveOnboardingStep1Status.ReadyForStep2, result.Status);
        Assert.Equal(2, session.CurrentStep);

        var saved = await context.OnboardingRequests.FirstAsync();
        Assert.Equal("New Hire", saved.FullName);
        Assert.Equal(RequestStatus.Draft, saved.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the CompleteOnboardingHrStep2Validator_Fails_WhenSessionMissing operation.
    /// </summary>
    public void CompleteOnboardingHrStep2Validator_Fails_WhenSessionMissing()
    {
        var validator = new CompleteOnboardingHrStep2CommandValidator();

        var result = validator.Validate(new CompleteOnboardingHrStep2Command(new OnboardingViewModel(), null!, "hr-user"));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the CompleteOnboardingHrStep2Handler_ReturnsMissingStep1_WhenSessionIncomplete operation.
    /// </summary>
    public async Task CompleteOnboardingHrStep2Handler_ReturnsMissingStep1_WhenSessionIncomplete()
    {
        using var context = CreateContext();
        var wizard = new Mock<IOnboardingWizardService>();
        var queue = new Mock<IEmailBackgroundQueue>();
        var handler = new CompleteOnboardingHrStep2CommandHandler(
            context,
            wizard.Object,
            queue.Object,
            new TestCurrentUserAccessor("hr-user", companies: ["HQ"]));

        var result = await handler.Handle(
            new CompleteOnboardingHrStep2Command(new OnboardingViewModel(), new OnboardingViewModel(), "hr-user"),
            CancellationToken.None);

        Assert.Equal(CompleteOnboardingHrStep2Status.MissingStep1, result.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the CompleteOnboardingHrStep2Handler_QueuesWelcomeEmail_WhenNewRequestIsSaved operation.
    /// </summary>
    public async Task CompleteOnboardingHrStep2Handler_QueuesWelcomeEmail_WhenNewRequestIsSaved()
    {
        using var context = CreateContext();
        var wizard = new Mock<IOnboardingWizardService>();
        var queue = new Mock<IEmailBackgroundQueue>();
        wizard.Setup(w => w.MapSessionToRequest(It.IsAny<OnboardingViewModel>(), It.IsAny<OnboardingRequest>()))
            .Callback<OnboardingViewModel, OnboardingRequest>((sessionModel, requestEntity) =>
            {
                requestEntity.FullName = sessionModel.FullName;
                requestEntity.EmployeeID = sessionModel.EmployeeID;
                requestEntity.Designation = sessionModel.Designation;
                requestEntity.Department = sessionModel.Department;
                requestEntity.Branch = sessionModel.Branch;
                requestEntity.HODEmail = sessionModel.HODEmail;
                requestEntity.EmailAddress = sessionModel.EmailAddress;
                requestEntity.ContactNumber = sessionModel.ContactNumber;
                requestEntity.DeskNumber = sessionModel.DeskNumber;
            });
        var handler = new CompleteOnboardingHrStep2CommandHandler(
            context,
            wizard.Object,
            queue.Object,
            new TestCurrentUserAccessor("hr-user", companies: ["HQ"]));
        var model = new OnboardingViewModel
        {
            IssuedAssets = new List<AssetItemViewModel>(),
            AssetRemarks = "Prepared"
        };
        var session = new OnboardingViewModel
        {
            FullName = "New Hire",
            EmployeeID = "EMP-100",
            Designation = "Engineer",
            Department = "IT",
            Branch = "HQ",
            HODEmail = "hod@example.com",
            EmailAddress = "new.hire@example.com",
            ContactNumber = "0123456789",
            DeskNumber = "D-10"
        };

        var result = await handler.Handle(
            new CompleteOnboardingHrStep2Command(model, session, "hr-user"),
            CancellationToken.None);

        Assert.Equal(CompleteOnboardingHrStep2Status.Completed, result.Status);
        queue.Verify(q => q.QueueEmailAsync(
                It.Is<IEmailBackgroundMessage>(m =>
                    m.GetType() == typeof(WelcomeEmailMessage) &&
                    ((WelcomeEmailMessage)m).Email == "new.hire@example.com" &&
                    ((WelcomeEmailMessage)m).EmployeeName == "New Hire"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetOnboardingStep1OptionsHandler_AllowsDepartmentWithoutCompanyMapping operation.
    /// </summary>
    public async Task GetOnboardingStep1OptionsHandler_AllowsDepartmentWithoutCompanyMapping()
    {
        using var context = CreateContext();
        var company = new Company { Id = 10, Name = "HQ", IsActive = true };
        context.Companies.Add(company);
        context.Departments.AddRange(
            new Department
            {
                Id = 1,
                Name = "IT",
                IsActive = true
            },
            new Department
            {
                Id = 2,
                Name = "HR",
                IsActive = true,
                DepartmentCompanies =
                {
                    new DepartmentCompany
                    {
                        CompanyId = 10,
                        Company = company
                    }
                }
            });
        await context.SaveChangesAsync();

        var hodService = new Mock<IOnboardingHodService>();
        hodService
            .Setup(x => x.BuildHodSelectListAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.AspNetCore.Mvc.Rendering.SelectList(Array.Empty<object>()));

        var handler = new GetOnboardingStep1OptionsQueryHandler(
            context,
            hodService.Object,
            new TestCurrentUserAccessor());

        var result = await handler.Handle(
            new GetOnboardingStep1OptionsQuery(new OnboardingViewModel()),
            CancellationToken.None);

        Assert.Equal(2, result.Departments.Count);
        Assert.Contains(result.Departments, d => d.Name == "IT" && d.CompanyName == null);
        Assert.Contains(result.Departments, d => d.Name == "HR" && d.CompanyName == "HQ");
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateOnboardingItStep3Validator_Fails_WhenSessionIdInvalid operation.
    /// </summary>
    public void UpdateOnboardingItStep3Validator_Fails_WhenSessionIdInvalid()
    {
        var validator = new UpdateOnboardingItStep3CommandValidator();

        var result = validator.Validate(new UpdateOnboardingItStep3Command(
            new OnboardingViewModel(),
            new OnboardingViewModel(),
            true));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateOnboardingItStep3Handler_SavesDraft_WhenPendingApproval operation.
    /// </summary>
    public async Task UpdateOnboardingItStep3Handler_SavesDraft_WhenPendingApproval()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 1, SoftwareName = "Microsoft Office", IsActive = true });
        context.SoftwareLicenses.Add(new SoftwareLicense { Id = 2, SoftwareName = "Microsoft Office", ContractLimit = 10 });
        context.EmailGroups.Add(new EmailGroup { Id = 3, GroupName = "All Staff", GroupEmail = "all@example.com", IsActive = true });
        context.SharedMailboxes.Add(new SharedMailbox { Id = 4, MailboxName = "IT", MailboxAddress = "it@example.com", IsActive = true });
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 1001,
            Status = RequestStatus.PendingITApproval,
            Branch = "HQ",
            RequestSoftware = new List<RequestSoftware>(),
            RequestEmailGroups = new List<RequestEmailGroup>(),
            RequestSharedMailboxes = new List<RequestSharedMailbox>(),
            IssuedAssets = new List<IssuedAsset>(),
            FolderPermissions = new List<FolderPermission>()
        });
        await context.SaveChangesAsync();

        var handler = new UpdateOnboardingItStep3CommandHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["HQ"]));
        var session = new OnboardingViewModel
        {
            Id = 1001,
            IssuedAssets = new List<AssetItemViewModel> { new() { AssetType = AssetType.Laptop, IsChecked = true } },
            FolderPermissions = new List<FolderPermissionViewModel>()
        };
        var model = new OnboardingViewModel
        {
            SelectedStandardSoftwareIds = new List<int> { 1 },
            SelectedSoftwareIds = new List<int> { 2 },
            SelectedEmailGroupIds = new List<int> { 3 },
            SelectedSharedMailboxIds = new List<int> { 4 },
            SoftwareRemarks = "software-ready",
            IssuedAssets = new List<AssetItemViewModel> { new() { ITAssetId = null, Qty = 1, Description = "Laptop" } }
        };

        var result = await handler.Handle(new UpdateOnboardingItStep3Command(model, session, true), CancellationToken.None);

        Assert.Equal(UpdateOnboardingItStep3Status.DraftSaved, result.Status);
        Assert.Equal(4, session.CurrentStep);

        var saved = await context.OnboardingRequests
            .Include(r => r.RequestSoftware)
            .Include(r => r.RequestEmailGroups)
            .Include(r => r.RequestSharedMailboxes)
            .FirstAsync(r => r.Id == 1001);
        Assert.Single(saved.RequestSoftware);
        Assert.Single(saved.RequestEmailGroups);
        Assert.Single(saved.RequestSharedMailboxes);
        Assert.True(session.HasMSOffice);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateOnboardingItStep4Validator_Fails_WhenSessionIdInvalid operation.
    /// </summary>
    public void UpdateOnboardingItStep4Validator_Fails_WhenSessionIdInvalid()
    {
        var validator = new UpdateOnboardingItStep4CommandValidator();

        var result = validator.Validate(new UpdateOnboardingItStep4Command(
            new OnboardingViewModel(),
            new OnboardingViewModel(),
            true));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateOnboardingItStep4Handler_SavesDraft_WhenPendingApproval operation.
    /// </summary>
    public async Task UpdateOnboardingItStep4Handler_SavesDraft_WhenPendingApproval()
    {
        using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 1002,
            Status = RequestStatus.PendingITApproval,
            Branch = "HQ",
            RequestSoftware = new List<RequestSoftware>(),
            RequestEmailGroups = new List<RequestEmailGroup>(),
            RequestSharedMailboxes = new List<RequestSharedMailbox>(),
            IssuedAssets = new List<IssuedAsset>(),
            FolderPermissions = new List<FolderPermission>()
        });
        await context.SaveChangesAsync();

        var handler = new UpdateOnboardingItStep4CommandHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["HQ"]));
        var session = new OnboardingViewModel
        {
            Id = 1002,
            FolderPermissions = new List<FolderPermissionViewModel>()
        };
        var model = new OnboardingViewModel
        {
            FolderPermissions = new List<FolderPermissionViewModel>
            {
                new() { FolderName = "Shared", CanRead = true, CanModify = true }
            },
            FolderRemarks = "folder-ready"
        };

        var result = await handler.Handle(new UpdateOnboardingItStep4Command(model, session, true), CancellationToken.None);

        Assert.Equal(UpdateOnboardingItStep4Status.DraftSaved, result.Status);
        Assert.Equal(5, session.CurrentStep);

        var saved = await context.OnboardingRequests
            .Include(r => r.FolderPermissions)
            .FirstAsync(r => r.Id == 1002);
        var folder = Assert.Single(saved.FolderPermissions);
        Assert.Equal("Shared", folder.FolderName);
        Assert.True(folder.CanModify);
    }

    /// <summary>
    /// Builds the value returned by the BuildUser operation.
    /// </summary>
    private static ClaimsPrincipal BuildUser(string username, string title, string? company = null)
    {
        var identity = new ClaimsIdentity("mock");
        identity.AddClaim(new Claim(ClaimTypes.Name, username));
        identity.AddClaim(new Claim("UserTitle", title));
        if (!string.IsNullOrWhiteSpace(company))
        {
            identity.AddClaim(new Claim("UserCompany", company));
        }

        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates data for the CreateUserManager operation.
    /// </summary>
    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    /// <summary>
    /// Creates data for the CreateContext operation.
    /// </summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var http = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        encryption.Setup(x => x.DecryptKey(It.IsAny<string>())).Returns((string s) => s);

        return new ApplicationDbContext(options, http.Object, logger.Object, encryption.Object);
    }
}

