using IT_Form.Application.Features.ITAsset.CreateItAsset;
using IT_Form.Application.Features.Onboarding.SaveOnboardingStep1;
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

public class CompanyScopeFeatureSliceTests
{
    [Fact]
    public async Task CreateItAssetHandler_AllowsItUserWithoutCompanyAssignment()
    {
        await using var context = CreateContext();
        var handler = new CreateItAssetCommandHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"]));

        var result = await handler.Handle(
            new CreateItAssetCommand(
                new ITAsset
                {
                    AssetTag = "TAG-100",
                    Company = "HQ",
                    Status = AssetStatus.Available
                },
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("IT asset created.", result.Message);
        Assert.Single(await context.ITAssets.ToListAsync());
    }

    [Fact]
    public async Task SaveOnboardingStep1Handler_ReturnsForbidden_WhenHrUserHasNoCompanyAssignment()
    {
        await using var context = CreateContext();
        var wizard = new Mock<IOnboardingWizardService>();
        var handler = new SaveOnboardingStep1CommandHandler(
            context,
            wizard.Object,
            new TestCurrentUserAccessor("hr-user", titles: ["HR"]));

        var result = await handler.Handle(
            new SaveOnboardingStep1Command(
                new OnboardingViewModel
                {
                    FullName = "New Hire",
                    EmployeeID = "EMP-100",
                    Designation = "Engineer",
                    Department = "IT",
                    Branch = "HQ",
                    HODEmail = "hod@example.com"
                },
                new OnboardingViewModel()),
            CancellationToken.None);

        Assert.Equal(SaveOnboardingStep1Status.Forbidden, result.Status);
        Assert.Equal("You can only create onboarding requests for your assigned company scope.", result.Message);
        Assert.Equal(0, await context.OnboardingRequests.CountAsync());
    }

    [Fact]
    public async Task SaveOnboardingStep1Handler_ReturnsForbidden_WhenHrUserTargetsDifferentCompany()
    {
        await using var context = CreateContext();
        var wizard = new Mock<IOnboardingWizardService>();
        var handler = new SaveOnboardingStep1CommandHandler(
            context,
            wizard.Object,
            new TestCurrentUserAccessor("hr-user", titles: ["HR"], companies: ["Alpha"]));

        var result = await handler.Handle(
            new SaveOnboardingStep1Command(
                new OnboardingViewModel
                {
                    FullName = "New Hire",
                    EmployeeID = "EMP-101",
                    Designation = "Engineer",
                    Department = "IT",
                    Branch = "Beta",
                    HODEmail = "hod@example.com"
                },
                new OnboardingViewModel()),
            CancellationToken.None);

        Assert.Equal(SaveOnboardingStep1Status.Forbidden, result.Status);
        Assert.Equal("You can only create onboarding requests for your assigned company scope.", result.Message);
        Assert.Equal(0, await context.OnboardingRequests.CountAsync());
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
