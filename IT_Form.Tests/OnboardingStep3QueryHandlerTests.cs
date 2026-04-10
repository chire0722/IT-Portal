using IT_Form.Application.Common.Mapping;
using IT_Form.Application.DTOs.Options;
using IT_Form.Application.Features.Onboarding.GetOnboardingItStep3;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class OnboardingStep3QueryHandlerTests
{
    public OnboardingStep3QueryHandlerTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task Handle_ReturnsOptionDtosForStep3Selections()
    {
        await using var context = CreateContext();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 10,
            Status = RequestStatus.PendingITApproval
        });

        context.StandardSoftware.Add(new StandardSoftware
        {
            Id = 1,
            SoftwareName = "Microsoft Office Product",
            IsActive = true,
            Licenses =
            [
                new SoftwareLicense
                {
                    Id = 2,
                    SoftwareName = "Microsoft Office E3",
                    StandardSoftwareId = 1,
                    ContractLimit = 5,
                    IsSubscription = true
                }
            ]
        });

        context.RequestSoftware.Add(new RequestSoftware
        {
            Id = 3,
            OnboardingRequestId = 10,
            StandardSoftwareId = 1,
            SoftwareName = "Microsoft Office Product"
        });

        context.EmailGroups.Add(new EmailGroup
        {
            Id = 4,
            GroupName = "All Staff",
            GroupEmail = "all@example.com",
            IsActive = true
        });

        context.SharedMailboxes.Add(new SharedMailbox
        {
            Id = 5,
            MailboxName = "IT",
            MailboxAddress = "it@example.com",
            IsActive = true
        });

        context.ITAssets.Add(new ITAsset
        {
            Id = 6,
            AssetTag = "LT-100",
            AssetType = AssetType.Laptop,
            Model = "Latitude 7440",
            SerialNumber = "SER-100",
            Status = AssetStatus.Available
        });

        await context.SaveChangesAsync();

        var handler = new GetOnboardingItStep3QueryHandler(context);
        var sessionModel = new OnboardingViewModel
        {
            Id = 10,
            CurrentStep = 2,
            FullName = "Alicia Tan",
            SelectedSoftwareIds = [2],
            SelectedStandardSoftwareIds = [1],
            SelectedEmailGroupIds = [4],
            SelectedSharedMailboxIds = [5],
            IssuedAssets =
            [
                new AssetItemViewModel
                {
                    AssetType = AssetType.Laptop,
                    IsChecked = true
                }
            ]
        };

        var result = await handler.Handle(new GetOnboardingItStep3Query(sessionModel), CancellationToken.None);

        Assert.Equal(GetOnboardingItStep3Status.Ready, result.Status);
        var viewModel = Assert.IsType<OnboardingStep3PageViewModel>(result.ViewModel);
        Assert.Equal(3, viewModel.CurrentStep);
        Assert.Single(viewModel.AvailableSoftware);
        Assert.IsType<SoftwareLicenseOptionDto>(viewModel.AvailableSoftware[0]);
        Assert.Equal("Microsoft Office E3", viewModel.AvailableSoftware[0].SoftwareName);
        Assert.Single(viewModel.StandardSoftwareOptions);
        Assert.IsType<StandardSoftwareOptionDto>(viewModel.StandardSoftwareOptions[0]);
        Assert.Single(viewModel.StandardSoftwareOptions[0].Licenses);
        Assert.Equal("Microsoft Office E3", viewModel.StandardSoftwareOptions[0].Licenses[0].SoftwareName);
        Assert.Single(viewModel.EmailGroups);
        Assert.IsType<EmailGroupOptionDto>(viewModel.EmailGroups[0]);
        Assert.Single(viewModel.SharedMailboxes);
        Assert.IsType<SharedMailboxOptionDto>(viewModel.SharedMailboxes[0]);
        Assert.Single(viewModel.AvailableAssets);
        Assert.IsType<ITAssetOptionDto>(viewModel.AvailableAssets[0]);
        Assert.Equal("LT-100", viewModel.AvailableAssets[0].AssetTag);
        Assert.Equal(5, viewModel.LicenseAvailability[2]);
        Assert.Equal([1], viewModel.RequestedSoftwareIds);
    }

    [Fact]
    public async Task Handle_ReturnsUnlimitedAvailability_ForFreeSoftware()
    {
        await using var context = CreateContext();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 20,
            Status = RequestStatus.PendingITApproval
        });

        context.StandardSoftware.Add(new StandardSoftware
        {
            Id = 21,
            SoftwareName = "Google Chrome",
            IsActive = true,
            Licenses =
            [
                new SoftwareLicense
                {
                    Id = 22,
                    SoftwareName = "Google Chrome",
                    StandardSoftwareId = 21,
                    IsFree = true
                }
            ]
        });

        await context.SaveChangesAsync();

        var handler = new GetOnboardingItStep3QueryHandler(context);
        var result = await handler.Handle(
            new GetOnboardingItStep3Query(new OnboardingViewModel { Id = 20, CurrentStep = 2 }),
            CancellationToken.None);

        Assert.Equal(GetOnboardingItStep3Status.Ready, result.Status);
        var viewModel = Assert.IsType<OnboardingStep3PageViewModel>(result.ViewModel);
        Assert.True(viewModel.AvailableSoftware[0].IsFree);
        Assert.Equal(int.MaxValue, viewModel.LicenseAvailability[22]);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>())).Returns((string value) => value);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>())).Returns((string value) => value);

        return new ApplicationDbContext(options, httpContextAccessor.Object, logger.Object, encryption.Object);
    }
}
