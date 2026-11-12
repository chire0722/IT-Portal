// Project comment: CompanyScopeFeatureSliceTests.cs is part of the IT Form application source code.

using IT_Form.Application.Features.ITAsset.CreateItAsset;
using IT_Form.Application.Features.Onboarding.ExportOnboardingCsv;
using IT_Form.Application.Features.Onboarding.SaveOnboardingStep1;
using System.Text;
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

/// <summary>
/// Defines the CompanyScopeFeatureSliceTests class used by the IT Form application.
/// </summary>
public class CompanyScopeFeatureSliceTests
{
    [Fact]
    /// <summary>
    /// Runs the ExportOnboardingCsvHandler_RequiresHrRole operation.
    /// </summary>
    public async Task ExportOnboardingCsvHandler_RequiresHrRole()
    {
        await using var context = CreateContext();
        var handler = new ExportOnboardingCsvQueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["Alpha"]));

        var result = await handler.Handle(
            new ExportOnboardingCsvQuery(null, null),
            CancellationToken.None);

        Assert.True(result.IsForbidden);
    }

    [Fact]
    /// <summary>
    /// Ensures onboarding CSV exports neutralize spreadsheet formula-like employee data.
    /// </summary>
    public async Task ExportOnboardingCsvHandler_SanitizesFormulaLikeValues()
    {
        await using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 902,
            FullName = "=HYPERLINK(\"https://example.invalid\")",
            EmployeeID = "+SUM(1,2)",
            Designation = "-1+1",
            Department = "@SUM(A1:A2)",
            Branch = "HQ",
            HODEmail = "\t@cmd"
        });
        await context.SaveChangesAsync();

        var handler = new ExportOnboardingCsvQueryHandler(
            context,
            new TestCurrentUserAccessor("hr-user", titles: ["HR"]));

        var result = await handler.Handle(
            new ExportOnboardingCsvQuery(null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var csv = Encoding.UTF8.GetString(result.Data!.Content);
        Assert.Contains("'=HYPERLINK(", csv);
        Assert.Contains("\\\"\'=HYPERLINK(\\\"\\\"https://example.invalid\\\"\\\")\\\"", csv);
        Assert.Contains("'+SUM(1,2)", csv);
        Assert.Contains("'-1+1", csv);
        Assert.Contains("'@SUM(A1:A2)", csv);
        Assert.Contains("'\t@cmd", csv);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateItAssetHandler_AllowsItUserWithoutCompanyAssignment operation.
    /// </summary>
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
    /// <summary>
    /// Runs the CreateItAssetHandler_RejectsIssuedAssetAssignmentAcrossCompanies operation.
    /// </summary>
    public async Task CreateItAssetHandler_RejectsIssuedAssetAssignmentAcrossCompanies()
    {
        await using var context = CreateContext();
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 901,
            FullName = "Beta Employee",
            Branch = "Beta",
            Status = RequestStatus.Completed
        });
        await context.SaveChangesAsync();

        var handler = new CreateItAssetCommandHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["HQ"]));

        var result = await handler.Handle(
            new CreateItAssetCommand(
                new ITAsset
                {
                    AssetTag = "TAG-101",
                    Company = "HQ",
                    Status = AssetStatus.Issued
                },
                901,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("same company", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.ITAssets.ToListAsync());
    }

    [Fact]
    /// <summary>
    /// Saves data for the SaveOnboardingStep1Handler_ReturnsForbidden_WhenHrUserHasNoCompanyAssignment operation.
    /// </summary>
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
    /// <summary>
    /// Saves data for the SaveOnboardingStep1Handler_ReturnsForbidden_WhenHrUserTargetsDifferentCompany operation.
    /// </summary>
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

    /// <summary>
    /// Creates data for the CreateContext operation.
    /// </summary>
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

