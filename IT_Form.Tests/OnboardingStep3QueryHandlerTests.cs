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
            Status = RequestStatus.PendingITApproval,
            Branch = "HQ"
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
            Status = AssetStatus.Available,
            Company = "HQ"
        });

        await context.SaveChangesAsync();

        var handler = new GetOnboardingItStep3QueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["HQ"]));
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
            Status = RequestStatus.PendingITApproval,
            Branch = "HQ"
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

        var handler = new GetOnboardingItStep3QueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["HQ"]));
        var result = await handler.Handle(
            new GetOnboardingItStep3Query(new OnboardingViewModel { Id = 20, CurrentStep = 2 }),
            CancellationToken.None);

        Assert.Equal(GetOnboardingItStep3Status.Ready, result.Status);
        var viewModel = Assert.IsType<OnboardingStep3PageViewModel>(result.ViewModel);
        Assert.True(viewModel.AvailableSoftware[0].IsFree);
        Assert.Equal(int.MaxValue, viewModel.LicenseAvailability[22]);
    }

    [Fact]
    public async Task Handle_FiltersSoftwareVersions_ByEmployeeCompany()
    {
        await using var context = CreateContext();

        context.Companies.AddRange(
            new Company { Id = 30, Name = "HQ", IsActive = true },
            new Company { Id = 31, Name = "Factory", IsActive = true });

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 40,
            Status = RequestStatus.PendingITApproval,
            Branch = "HQ"
        });

        context.StandardSoftware.AddRange(
            new StandardSoftware
            {
                Id = 41,
                SoftwareName = "Adobe Acrobat",
                IsActive = true,
                Licenses =
                [
                    new SoftwareLicense
                    {
                        Id = 42,
                        SoftwareName = "Adobe Global",
                        StandardSoftwareId = 41
                    },
                    new SoftwareLicense
                    {
                        Id = 43,
                        SoftwareName = "Adobe HQ",
                        StandardSoftwareId = 41,
                        SoftwareLicenseCompanies =
                        [
                            new SoftwareLicenseCompany
                            {
                                CompanyId = 30
                            }
                        ]
                    },
                    new SoftwareLicense
                    {
                        Id = 44,
                        SoftwareName = "Adobe Factory",
                        StandardSoftwareId = 41,
                        SoftwareLicenseCompanies =
                        [
                            new SoftwareLicenseCompany
                            {
                                CompanyId = 31
                            }
                        ]
                    }
                ]
            },
            new StandardSoftware
            {
                Id = 45,
                SoftwareName = "Factory Only",
                IsActive = true,
                Licenses =
                [
                    new SoftwareLicense
                    {
                        Id = 46,
                        SoftwareName = "Factory Locked",
                        StandardSoftwareId = 45,
                        SoftwareLicenseCompanies =
                        [
                            new SoftwareLicenseCompany
                            {
                                CompanyId = 31
                            }
                        ]
                    }
                ]
            });

        context.EmailGroups.AddRange(
            new EmailGroup
            {
                Id = 47,
                GroupName = "All Staff",
                GroupEmail = "all@global.test",
                IsActive = true
            },
            new EmailGroup
            {
                Id = 48,
                GroupName = "HQ Staff",
                GroupEmail = "hq@alpha.test",
                IsActive = true,
                EmailGroupCompanies =
                [
                    new EmailGroupCompany
                    {
                        CompanyId = 30
                    }
                ]
            },
            new EmailGroup
            {
                Id = 49,
                GroupName = "Factory Staff",
                GroupEmail = "factory@beta.test",
                IsActive = true,
                EmailGroupCompanies =
                [
                    new EmailGroupCompany
                    {
                        CompanyId = 31
                    }
                ]
            });

        context.SharedMailboxes.AddRange(
            new SharedMailbox
            {
                Id = 50,
                MailboxName = "General",
                MailboxAddress = "general@global.test",
                IsActive = true
            },
            new SharedMailbox
            {
                Id = 51,
                MailboxName = "HQ Ops",
                MailboxAddress = "hq.ops@alpha.test",
                IsActive = true,
                SharedMailboxCompanies =
                [
                    new SharedMailboxCompany
                    {
                        CompanyId = 30
                    }
                ]
            },
            new SharedMailbox
            {
                Id = 52,
                MailboxName = "Factory Ops",
                MailboxAddress = "factory.ops@beta.test",
                IsActive = true,
                SharedMailboxCompanies =
                [
                    new SharedMailboxCompany
                    {
                        CompanyId = 31
                    }
                ]
            });

        context.ITAssets.AddRange(
            new ITAsset
            {
                Id = 53,
                AssetTag = "HQ-LT-1",
                AssetType = AssetType.Laptop,
                Status = AssetStatus.Available,
                Company = "HQ"
            },
            new ITAsset
            {
                Id = 54,
                AssetTag = "FAC-LT-1",
                AssetType = AssetType.Laptop,
                Status = AssetStatus.Available,
                Company = "Factory"
            });

        await context.SaveChangesAsync();

        var handler = new GetOnboardingItStep3QueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["HQ"]));
        var result = await handler.Handle(
            new GetOnboardingItStep3Query(new OnboardingViewModel { Id = 40, CurrentStep = 2 }),
            CancellationToken.None);

        Assert.Equal(GetOnboardingItStep3Status.Ready, result.Status);
        var viewModel = Assert.IsType<OnboardingStep3PageViewModel>(result.ViewModel);
        Assert.Equal([42, 43], viewModel.AvailableSoftware.Select(license => license.Id).OrderBy(id => id).ToArray());
        Assert.Single(viewModel.StandardSoftwareOptions);
        Assert.Equal(41, viewModel.StandardSoftwareOptions[0].Id);
        Assert.Equal([42, 43], viewModel.StandardSoftwareOptions[0].Licenses.Select(license => license.Id).OrderBy(id => id).ToArray());
        Assert.Equal([47, 48], viewModel.EmailGroups.Select(group => group.Id).OrderBy(id => id).ToArray());
        Assert.Equal([50, 51], viewModel.SharedMailboxes.Select(mailbox => mailbox.Id).OrderBy(id => id).ToArray());
        Assert.Equal([53], viewModel.AvailableAssets.Select(asset => asset.Id).ToArray());
    }

    [Fact]
    public async Task Handle_ReturnsForbidden_WhenItUserTargetsDifferentCompany()
    {
        await using var context = CreateContext();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 60,
            Status = RequestStatus.PendingITApproval,
            Branch = "Factory"
        });

        await context.SaveChangesAsync();

        var handler = new GetOnboardingItStep3QueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["HQ"]));

        var result = await handler.Handle(
            new GetOnboardingItStep3Query(new OnboardingViewModel { Id = 60, CurrentStep = 2 }),
            CancellationToken.None);

        Assert.Equal(GetOnboardingItStep3Status.Forbidden, result.Status);
        Assert.Equal(60, result.RequestId);
        Assert.Null(result.ViewModel);
        Assert.Contains("assigned company scope", result.Message);
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
