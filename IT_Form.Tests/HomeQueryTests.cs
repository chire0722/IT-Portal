// Project comment: HomeQueryTests.cs is part of the IT Form application source code.

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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the HomeQueryTests class used by the IT Form application.
/// </summary>
public class HomeQueryTests
{
    public HomeQueryTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Runs the AnalyticsQuery_ReturnsForbidden_ForNonHrAndNonItUsers operation.
    /// </summary>
    public async Task AnalyticsQuery_ReturnsForbidden_ForNonHrAndNonItUsers()
    {
        await using var context = CreateContext();
        var handler = new GetHomeAnalyticsQueryHandler(
            context,
            new TestCurrentUserAccessor("manager@company.com", "manager-1", "Manager"));

        var result = await handler.Handle(new GetHomeAnalyticsQuery(2025, null), CancellationToken.None);

        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task AnalyticsQuery_PopulatesSuperAdminOnlyAnalytics_ForSuperAdmin()
    {
        await using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = false });
        context.Users.AddRange(
            new ApplicationUser { Id = "active", UserName = "active", FullName = "Active User", IsActive = true },
            new ApplicationUser { Id = "locked", UserName = "locked", FullName = "Locked User", IsActive = true, LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30) },
            new ApplicationUser { Id = "inactive", UserName = "inactive", FullName = "Inactive User", IsActive = false });
        context.OtpRecords.Add(new OtpRecord
        {
            Email = "super@example.com",
            Code = "123456",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            FailedAttempts = 2
        });
        context.AuditLogs.AddRange(
            new AuditLog
            {
                UserId = "super",
                PerformedBy = "super@example.com",
                EntityName = "ControlPlane",
                EntityId = "MaintenanceMode",
                Action = "EnableMaintenanceMode",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                IsSuccess = true
            },
            new AuditLog
            {
                UserId = "unknown",
                PerformedBy = "unknown",
                EntityName = "Account",
                EntityId = "Login",
                Action = "Login",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                IsSuccess = false
            });
        context.SystemSettings.AddRange(
            new SystemSetting { Key = "ControlPlane.Maintenance.Enabled", Value = bool.TrueString },
            new SystemSetting { Key = "ControlPlane.Maintenance.Reason", Value = "Planned work" },
            new SystemSetting { Key = "ControlPlane.Quota.companies.Limit", Value = "1" },
            new SystemSetting { Key = "ControlPlane.Quota.companies.ReadOnly", Value = bool.TrueString });
        await context.SaveChangesAsync();

        var handler = new GetHomeAnalyticsQueryHandler(
            context,
            new TestCurrentUserAccessor("super@example.com", "super", "SuperAdmin"));

        var result = await handler.Handle(new GetHomeAnalyticsQuery(2026, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = result.Data!.ViewModel;
        Assert.Equal(1, model.SuperAdminActiveCompanyCount);
        Assert.Equal(1, model.SuperAdminInactiveCompanyCount);
        Assert.Equal(2, model.SuperAdminActiveUserCount);
        Assert.Equal(1, model.SuperAdminInactiveUserCount);
        Assert.Equal(1, model.SuperAdminLockedUserCount);
        Assert.Equal(1, model.SuperAdminFailedLoginCount24h);
        Assert.Equal(1, model.SuperAdminFailedOtpCount24h);
        Assert.True(model.SuperAdminMaintenanceModeEnabled);
        Assert.Equal("Planned work", model.SuperAdminMaintenanceModeReason);
        Assert.Contains("Companies", model.SuperAdminQuotaUsage.Keys);
        Assert.Contains(model.SuperAdminRecentControlEvents, item => item.Action == "EnableMaintenanceMode");
    }

    [Fact]
    /// <summary>
    /// Runs the AnalyticsQuery_BuildsMonthlyTrendFromGroupedQueries operation.
    /// </summary>
    public async Task AnalyticsQuery_BuildsMonthlyTrendFromGroupedQueries()
    {
        await using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 1,
                FullName = "Alice",
                Department = "HR",
                Branch = "Alpha",
                RequestDate = new DateTime(2025, 1, 5, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 2,
                FullName = "Bob",
                Department = "IT",
                Branch = "Alpha",
                RequestDate = new DateTime(2025, 1, 20, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 3,
                FullName = "Cara",
                Department = "Finance",
                Branch = "Beta",
                RequestDate = new DateTime(2025, 2, 1, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 4,
                FullName = "Old Year",
                Department = "HR",
                Branch = "Alpha",
                RequestDate = new DateTime(2024, 12, 31, 8, 0, 0, DateTimeKind.Utc)
            });
        context.OffboardingRequests.Add(
            new OffboardingRequest
            {
                Id = 10,
                FullName = "Dana",
                Department = "IT",
                Branch = "Beta",
                HODEmail = "hod@company.com",
                LastDayOfWork = new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                RequestDate = new DateTime(2025, 2, 14, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var handler = new GetHomeAnalyticsQueryHandler(
            context,
            new TestCurrentUserAccessor("it@company.com", "it-1", "IT", new[] { "Alpha", "Beta" }));

        var result = await handler.Handle(new GetHomeAnalyticsQuery(2025, null), CancellationToken.None);

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

    [Fact]
    /// <summary>
    /// Runs the AnalyticsQuery_FiltersMetricsBySelectedCompany operation.
    /// </summary>
    public async Task AnalyticsQuery_FiltersMetricsBySelectedCompany()
    {
        await using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });

        context.StandardSoftware.AddRange(
            new StandardSoftware { Id = 1, SoftwareName = "Alpha Suite", IsActive = true },
            new StandardSoftware { Id = 2, SoftwareName = "Beta Suite", IsActive = true },
            new StandardSoftware { Id = 3, SoftwareName = "Global Tools", IsActive = true });

        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 1,
                FullName = "Alice",
                Department = "IT",
                Branch = "Alpha",
                Designation = "Engineer",
                Status = IT_Form.Models.Enums.RequestStatus.Completed,
                RequestDate = new DateTime(2025, 1, 5, 8, 0, 0, DateTimeKind.Utc),
                RequesterSignedAt = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc),
                ITAdminSignedAt = new DateTime(2025, 1, 6, 10, 0, 0, DateTimeKind.Utc),
                EmployeeSignedAt = new DateTime(2025, 1, 7, 10, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2025, 1, 7, 10, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 2,
                FullName = "Bob",
                Department = "IT",
                Branch = "Beta",
                Designation = "Technician",
                Status = IT_Form.Models.Enums.RequestStatus.Completed,
                RequestDate = new DateTime(2025, 2, 10, 8, 0, 0, DateTimeKind.Utc),
                RequesterSignedAt = new DateTime(2025, 2, 10, 10, 0, 0, DateTimeKind.Utc),
                ITAdminSignedAt = new DateTime(2025, 2, 11, 10, 0, 0, DateTimeKind.Utc),
                EmployeeSignedAt = new DateTime(2025, 2, 12, 10, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2025, 2, 12, 10, 0, 0, DateTimeKind.Utc)
            });

        context.OffboardingRequests.AddRange(
            new OffboardingRequest
            {
                Id = 10,
                FullName = "Dana",
                Department = "IT",
                Branch = "Alpha",
                HODEmail = "hod@alpha.com",
                LastDayOfWork = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = IT_Form.Models.Enums.RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2025, 2, 14, 8, 0, 0, DateTimeKind.Utc)
            },
            new OffboardingRequest
            {
                Id = 11,
                FullName = "Evan",
                Department = "IT",
                Branch = "Beta",
                HODEmail = "hod@beta.com",
                LastDayOfWork = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = IT_Form.Models.Enums.RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2025, 3, 14, 8, 0, 0, DateTimeKind.Utc)
            });

        context.ITAssets.AddRange(
            new ITAsset
            {
                Id = 1,
                AssetTag = "A-001",
                AssetType = IT_Form.Models.Enums.AssetType.Laptop,
                Status = IT_Form.Models.Enums.AssetStatus.Available,
                Company = "Alpha",
                WindowsVersion = "Windows 11 Pro",
                PurchaseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                WarrantyExpiry = DateTime.UtcNow.Date.AddDays(15)
            },
            new ITAsset
            {
                Id = 2,
                AssetTag = "B-001",
                AssetType = IT_Form.Models.Enums.AssetType.DesktopPC,
                Status = IT_Form.Models.Enums.AssetStatus.Issued,
                Company = "Beta",
                WindowsVersion = "Windows 10 Pro",
                PurchaseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                WarrantyExpiry = DateTime.UtcNow.Date.AddDays(45)
            });

        context.MaintenanceLogs.AddRange(
            new MaintenanceLog
            {
                Id = 1,
                ITAssetId = 1,
                ServiceDate = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                Description = "Alpha maintenance"
            },
            new MaintenanceLog
            {
                Id = 2,
                ITAssetId = 2,
                ServiceDate = new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                Description = "Beta maintenance"
            });

        context.SoftwareLicenses.AddRange(
            new SoftwareLicense
            {
                Id = 1,
                SoftwareName = "Alpha Suite",
                StandardSoftwareId = 1,
                IsSubscription = true,
                ContractLimit = 3,
                SoftwareLicenseCompanies = [new SoftwareLicenseCompany { CompanyId = 1 }]
            },
            new SoftwareLicense
            {
                Id = 2,
                SoftwareName = "Beta Suite",
                StandardSoftwareId = 2,
                IsSubscription = true,
                ContractLimit = 2,
                SoftwareLicenseCompanies = [new SoftwareLicenseCompany { CompanyId = 2 }]
            },
            new SoftwareLicense
            {
                Id = 3,
                SoftwareName = "Global Tools",
                StandardSoftwareId = 3,
                IsSubscription = true,
                ContractLimit = 1
            });

        context.SoftwareKeys.AddRange(
            new SoftwareKey { Id = 1, SoftwareLicenseId = 1, IsAssigned = true },
            new SoftwareKey { Id = 2, SoftwareLicenseId = 2, IsAssigned = true });

        context.Users.AddRange(
            new ApplicationUser
            {
                Id = "user-a",
                UserName = "alpha.user",
                FullName = "Alpha User",
                IsActive = true,
                UserCompanies = [new UserCompany { CompanyName = "Alpha" }]
            },
            new ApplicationUser
            {
                Id = "user-b",
                UserName = "beta.user",
                FullName = "Beta User",
                IsActive = true,
                UserCompanies = [new UserCompany { CompanyName = "Beta" }]
            });

        await context.SaveChangesAsync();

        var handler = new GetHomeAnalyticsQueryHandler(
            context,
            new TestCurrentUserAccessor("it@company.com", "it-1", "IT", new[] { "Alpha", "Beta" }));

        var result = await handler.Handle(new GetHomeAnalyticsQuery(2025, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data!.SelectedCompanyId);
        Assert.Equal("Alpha", result.Data.SelectedCompanyDisplayName);
        Assert.Equal(2, result.Data.CompanyOptions.Count);
        Assert.Equal(1, result.Data.ViewModel.MonthlyTrendOnboarding[0]);
        Assert.Equal(0, result.Data.ViewModel.MonthlyTrendOnboarding[1]);
        Assert.Equal(1, result.Data.ViewModel.TotalAssets);
        Assert.Equal(1, result.Data.ViewModel.MaintenanceCountYear);
        Assert.Equal(1, result.Data.ViewModel.TotalActiveUsers);
        Assert.Equal(4, result.Data.ViewModel.TotalSoftwareLicenses);
        Assert.Equal(1, result.Data.ViewModel.AssignedSoftwareLicenses);
        Assert.Equal(2, result.Data.ViewModel.TotalLicenses);
        Assert.Equal(1, result.Data.ViewModel.BranchOnboardingBreakdown["Alpha"]);
        Assert.DoesNotContain("Beta", result.Data.ViewModel.BranchOnboardingBreakdown.Keys);
        Assert.DoesNotContain(result.Data.ViewModel.LicenseUtilization, item => item.SoftwareName == "Beta Suite");
        Assert.Contains(result.Data.ViewModel.LicenseUtilization, item => item.SoftwareName == "Alpha Suite");
        Assert.Contains(result.Data.ViewModel.LicenseUtilization, item => item.SoftwareName == "Global Tools");
    }

    [Fact]
    /// <summary>
    /// Runs the AnalyticsQuery_UsesAssignedCompanyScope_WhenNoCompanyIsSelected operation.
    /// </summary>
    public async Task AnalyticsQuery_UsesAssignedCompanyScope_WhenNoCompanyIsSelected()
    {
        await using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });

        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 1,
                FullName = "Alice",
                Department = "IT",
                Branch = "Alpha",
                RequestDate = new DateTime(2025, 1, 5, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 2,
                FullName = "Bob",
                Department = "IT",
                Branch = "Beta",
                RequestDate = new DateTime(2025, 2, 5, 8, 0, 0, DateTimeKind.Utc)
            });

        context.ITAssets.AddRange(
            new ITAsset
            {
                Id = 1,
                AssetTag = "A-001",
                AssetType = IT_Form.Models.Enums.AssetType.Laptop,
                Company = "Alpha"
            },
            new ITAsset
            {
                Id = 2,
                AssetTag = "B-001",
                AssetType = IT_Form.Models.Enums.AssetType.DesktopPC,
                Company = "Beta"
            });

        context.Users.AddRange(
            new ApplicationUser
            {
                Id = "user-a",
                UserName = "alpha.user",
                FullName = "Alpha User",
                IsActive = true,
                UserCompanies = [new UserCompany { CompanyName = "Alpha" }]
            },
            new ApplicationUser
            {
                Id = "user-b",
                UserName = "beta.user",
                FullName = "Beta User",
                IsActive = true,
                UserCompanies = [new UserCompany { CompanyName = "Beta" }]
            });

        await context.SaveChangesAsync();

        var handler = new GetHomeAnalyticsQueryHandler(
            context,
            new TestCurrentUserAccessor("it@company.com", "it-1", "IT", new[] { "Alpha" }));

        var result = await handler.Handle(new GetHomeAnalyticsQuery(2025, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.Data!.SelectedCompanyId);
        Assert.Single(result.Data.CompanyOptions);
        Assert.Equal(1, result.Data.CompanyOptions[0].Id);
        Assert.Equal("Alpha", result.Data.CompanyOptions[0].DisplayName);
        Assert.Equal(1, result.Data.ViewModel.MonthlyTrendOnboarding[0]);
        Assert.Equal(0, result.Data.ViewModel.MonthlyTrendOnboarding[1]);
        Assert.Equal(1, result.Data.ViewModel.TotalAssets);
        Assert.Equal(1, result.Data.ViewModel.TotalActiveUsers);
        Assert.Single(result.Data.ViewModel.BranchOnboardingBreakdown);
        Assert.Equal(1, result.Data.ViewModel.BranchOnboardingBreakdown["Alpha"]);
    }

    [Fact]
    /// <summary>
    /// Runs the DashboardQuery_FiltersPendingQueues_ByItAssignedCompany operation.
    /// </summary>
    public async Task DashboardQuery_FiltersPendingQueues_ByItAssignedCompany()
    {
        await using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });

        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 1,
                FullName = "Alpha Hire",
                Department = "IT",
                Branch = "Alpha",
                Status = IT_Form.Models.Enums.RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 2,
                FullName = "Beta Hire",
                Department = "IT",
                Branch = "Beta",
                Status = IT_Form.Models.Enums.RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 3,
                FullName = "Alpha Awaiting Sign",
                Department = "IT",
                Branch = "Alpha",
                Status = IT_Form.Models.Enums.RequestStatus.PendingEmployeeSign,
                RequestDate = new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 4, 4, 8, 0, 0, DateTimeKind.Utc)
            },
            new OnboardingRequest
            {
                Id = 4,
                FullName = "Beta Awaiting Sign",
                Department = "IT",
                Branch = "Beta",
                Status = IT_Form.Models.Enums.RequestStatus.PendingEmployeeSign,
                RequestDate = new DateTime(2026, 4, 5, 8, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 4, 6, 8, 0, 0, DateTimeKind.Utc)
            });

        context.OffboardingRequests.AddRange(
            new OffboardingRequest
            {
                Id = 10,
                FullName = "Alpha Exit",
                Department = "IT",
                Branch = "Alpha",
                HODEmail = "alpha@example.com",
                LastDayOfWork = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                Status = IT_Form.Models.Enums.RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 7, 8, 0, 0, DateTimeKind.Utc)
            },
            new OffboardingRequest
            {
                Id = 11,
                FullName = "Beta Exit",
                Department = "IT",
                Branch = "Beta",
                HODEmail = "beta@example.com",
                LastDayOfWork = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                Status = IT_Form.Models.Enums.RequestStatus.PendingITApproval,
                RequestDate = new DateTime(2026, 4, 8, 8, 0, 0, DateTimeKind.Utc)
            },
            new OffboardingRequest
            {
                Id = 12,
                FullName = "Alpha Exit Awaiting Sign",
                Department = "IT",
                Branch = "Alpha",
                HODEmail = "alpha@example.com",
                LastDayOfWork = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                Status = IT_Form.Models.Enums.RequestStatus.PendingEmployeeSign,
                RequestDate = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc)
            },
            new OffboardingRequest
            {
                Id = 13,
                FullName = "Beta Exit Awaiting Sign",
                Department = "IT",
                Branch = "Beta",
                HODEmail = "beta@example.com",
                LastDayOfWork = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                Status = IT_Form.Models.Enums.RequestStatus.PendingEmployeeSign,
                RequestDate = new DateTime(2026, 4, 11, 8, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 4, 12, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetHomeDashboardQueryHandler(
            context,
            new TestCurrentUserAccessor("it@company.com", "it-1", "IT", new[] { "Alpha" }),
            cache);

        var result = await handler.Handle(new GetHomeDashboardQuery(null), CancellationToken.None);

        Assert.Equal(["Alpha Hire"], result.ViewModel.PendingOnboarding.Select(item => item.FullName!).ToArray());
        Assert.Equal(["Alpha Exit"], result.ViewModel.PendingOffboarding.Select(item => item.FullName).ToArray());
        Assert.Equal(["Alpha Awaiting Sign"], result.ViewModel.PendingEmployeeSignOnboarding.Select(item => item.FullName!).ToArray());
        Assert.Equal(["Alpha Exit Awaiting Sign"], result.ViewModel.PendingEmployeeSignOffboarding.Select(item => item.FullName!).ToArray());
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
    /// Defines the TestCurrentUserAccessor class used by the IT Form application.
    /// </summary>
    private sealed class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        public TestCurrentUserAccessor(string userName, string userId, params string[] titles)
            : this(userName, userId, titles, null)
        {
        }

        public TestCurrentUserAccessor(string userName, string userId, string title, IEnumerable<string> companies)
            : this(userName, userId, new[] { title }, companies)
        {
        }

        private TestCurrentUserAccessor(string userName, string userId, IEnumerable<string> titles, IEnumerable<string>? companies)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.NameIdentifier, userId)
            };

            claims.AddRange(titles.Select(title => new Claim("UserTitle", title)));
            claims.AddRange((companies ?? Array.Empty<string>()).Select(company => new Claim("UserCompany", company)));
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            UserName = userName;
        }

        public ClaimsPrincipal User { get; }

        public string? UserName { get; }
    }
}
