// Project comment: RecordIndexScopeTests.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the RecordIndexScopeTests class used by the IT Form application.
/// </summary>
public class RecordIndexScopeTests
{
    public RecordIndexScopeTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Runs the OnboardingIndex_HrUserSeesRecordsAcrossCompanies operation.
    /// </summary>
    public async Task OnboardingIndex_HrUserSeesRecordsAcrossCompanies()
    {
        await using var context = CreateContext();
        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 1,
                FullName = "Alpha Hire",
                Branch = "Alpha",
                Status = RequestStatus.Draft,
                RequestDate = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 2,
                FullName = "Beta Hire",
                Branch = "Beta",
                Status = RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var handler = new GetOnboardingIndexQueryHandler(
            context,
            new TestCurrentUserAccessor("hr-user", "hr-1", titles: ["HR"], companies: ["Alpha"]));

        var result = await handler.Handle(new GetOnboardingIndexQuery(null, null, 1), CancellationToken.None);

        Assert.Equal(2, result.Requests.Count);
        Assert.Contains(result.Requests, request => request.Id == 1 && request.Branch == "Alpha");
        Assert.Contains(result.Requests, request => request.Id == 2 && request.Branch == "Beta");
    }

    [Fact]
    /// <summary>
    /// Runs the OnboardingIndex_ItUserSeesOnlyAssignedCompanyRecords operation.
    /// </summary>
    public async Task OnboardingIndex_ItUserSeesOnlyAssignedCompanyRecords()
    {
        await using var context = CreateContext();
        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 3,
                FullName = "Alpha Hire",
                Branch = "Alpha",
                Status = RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 5, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 4,
                FullName = "Beta Hire",
                Branch = "Beta",
                Status = RequestStatus.Completed,
                RequestDate = new DateTime(2026, 4, 6, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var handler = new GetOnboardingIndexQueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", "it-1", titles: ["IT"], companies: ["Alpha"]));

        var result = await handler.Handle(new GetOnboardingIndexQuery(null, null, 1), CancellationToken.None);

        Assert.Single(result.Requests);
        Assert.Contains(result.Requests, request => request.Id == 3 && request.Branch == "Alpha");
    }

    [Fact]
    /// <summary>
    /// Runs the OffboardingIndex_HrUserSeesRecordsAcrossCompanies operation.
    /// </summary>
    public async Task OffboardingIndex_HrUserSeesRecordsAcrossCompanies()
    {
        await using var context = CreateContext();
        context.OffboardingRequests.AddRange(
            new OffboardingRequest
            {
                Id = 10,
                FullName = "Alpha Exit",
                Branch = "Alpha",
                Department = "IT",
                HODEmail = "hod@alpha.test",
                LastDayOfWork = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                Status = RequestStatus.PendingHODApproval,
                RequestDate = new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc)
            },
            new OffboardingRequest
            {
                Id = 11,
                FullName = "Beta Exit",
                Branch = "Beta",
                Department = "HR",
                HODEmail = "hod@beta.test",
                LastDayOfWork = new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
                Status = RequestStatus.Completed,
                RequestDate = new DateTime(2026, 4, 11, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var handler = new GetOffboardingIndexQueryHandler(
            context,
            new TestCurrentUserAccessor("hr-user", "hr-1", titles: ["HR"], companies: ["Alpha"]),
            CreateUserManager());

        var result = await handler.Handle(new GetOffboardingIndexQuery(null, null, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Requests.Count);
        Assert.Contains(result.Data.Requests, request => request.Id == 10 && request.Branch == "Alpha");
        Assert.Contains(result.Data.Requests, request => request.Id == 11 && request.Branch == "Beta");
    }

    [Fact]
    /// <summary>
    /// Runs the OffboardingIndex_ItUserSeesOnlyAssignedCompanyRecords operation.
    /// </summary>
    public async Task OffboardingIndex_ItUserSeesOnlyAssignedCompanyRecords()
    {
        await using var context = CreateContext();
        context.OffboardingRequests.AddRange(
            new OffboardingRequest
            {
                Id = 12,
                FullName = "Alpha Exit",
                Branch = "Alpha",
                Department = "IT",
                HODEmail = "hod@alpha.test",
                LastDayOfWork = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc),
                Status = RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 12, 8, 0, 0, DateTimeKind.Utc)
            },
            new OffboardingRequest
            {
                Id = 13,
                FullName = "Beta Exit",
                Branch = "Beta",
                Department = "HR",
                HODEmail = "hod@beta.test",
                LastDayOfWork = new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc),
                Status = RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 13, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var handler = new GetOffboardingIndexQueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", "it-1", titles: ["IT"], companies: ["Alpha"]),
            CreateUserManager());

        var result = await handler.Handle(new GetOffboardingIndexQuery(null, null, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Requests);
        Assert.Contains(result.Data.Requests, request => request.Id == 12 && request.Branch == "Alpha");
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

    /// <summary>
    /// Creates data for the CreateUserManager operation.
    /// </summary>
    private static UserManager<ApplicationUser> CreateUserManager()
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
            null!).Object;
    }
}
