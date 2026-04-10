using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Home;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class HomeQueryTests
{
    public HomeQueryTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task AnalyticsQuery_ReturnsForbidden_ForNonHrAndNonItUsers()
    {
        await using var context = CreateContext();
        var handler = new GetHomeAnalyticsQueryHandler(
            context,
            new TestCurrentUserAccessor("manager@company.com", "manager-1", "Manager"));

        var result = await handler.Handle(new GetHomeAnalyticsQuery(2025), CancellationToken.None);

        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task AnalyticsQuery_BuildsMonthlyTrendFromGroupedQueries()
    {
        await using var context = CreateContext();
        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 1,
                FullName = "Alice",
                Department = "HR",
                RequestDate = new DateTime(2025, 1, 5, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 2,
                FullName = "Bob",
                Department = "IT",
                RequestDate = new DateTime(2025, 1, 20, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 3,
                FullName = "Cara",
                Department = "Finance",
                RequestDate = new DateTime(2025, 2, 1, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 4,
                FullName = "Old Year",
                Department = "HR",
                RequestDate = new DateTime(2024, 12, 31, 8, 0, 0, DateTimeKind.Utc)
            });
        context.OffboardingRequests.Add(
            new OffboardingRequest
            {
                Id = 10,
                FullName = "Dana",
                Department = "IT",
                HODEmail = "hod@company.com",
                LastDayOfWork = new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                RequestDate = new DateTime(2025, 2, 14, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var handler = new GetHomeAnalyticsQueryHandler(
            context,
            new TestCurrentUserAccessor("it@company.com", "it-1", "IT"));

        var result = await handler.Handle(new GetHomeAnalyticsQuery(2025), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("In 2025", result.Data!.StatsPeriodLabel);
        Assert.Equal("Jan 2025", result.Data.ViewModel.MonthlyTrendLabels[0]);
        Assert.Equal("Feb 2025", result.Data.ViewModel.MonthlyTrendLabels[1]);
        Assert.Equal(2, result.Data.ViewModel.MonthlyTrendOnboarding[0]);
        Assert.Equal(1, result.Data.ViewModel.MonthlyTrendOnboarding[1]);
        Assert.Equal(0, result.Data.ViewModel.MonthlyTrendOffboarding[0]);
        Assert.Equal(1, result.Data.ViewModel.MonthlyTrendOffboarding[1]);
        Assert.Equal(2, result.Data.ViewModel.MonthlyNetGrowth[0]);
        Assert.Equal(0, result.Data.ViewModel.MonthlyNetGrowth[1]);
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

    private sealed class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        public TestCurrentUserAccessor(string userName, string userId, params string[] titles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.NameIdentifier, userId)
            };

            claims.AddRange(titles.Select(title => new Claim("UserTitle", title)));
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            UserName = userName;
        }

        public ClaimsPrincipal User { get; }

        public string? UserName { get; }
    }
}
