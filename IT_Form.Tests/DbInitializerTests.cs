// Project comment: DbInitializerTests.cs validates startup database seeding behavior.

using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Security;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IT_Form.Tests;

public sealed class DbInitializerTests
{
    [Fact]
    public async Task SeedUsersAsync_CreatesConfiguredSuperAdmin_WhenConfiguredEmailDoesNotExist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var provider = CreateServiceProvider(connection);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = CreateConfiguration("owner@example.com", "Configured123!");

        await DbInitializer.SeedUsersAsync(userManager, context, configuration);

        var superAdmin = await context.Users.SingleAsync(user => user.Email == "owner@example.com");
        var titles = await context.UserTitles
            .Where(title => title.UserId == superAdmin.Id)
            .Select(title => title.Title)
            .ToListAsync();

        Assert.Contains(PortalTitles.SuperAdmin, titles);
        Assert.Contains(PortalTitles.IT, titles);
        Assert.True(superAdmin.EmailConfirmed);
        Assert.True(superAdmin.IsActive);
        Assert.False(superAdmin.MustChangePassword);
        Assert.True(await userManager.CheckPasswordAsync(superAdmin, "Configured123!"));
        Assert.Empty(await context.UserTitles
            .Where(title => title.Title == PortalTitles.SuperAdmin && title.UserId != superAdmin.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task SeedUsersAsync_ReplacesPreviousBootstrapSuperAdmin_WhenConfiguredEmailChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var provider = CreateServiceProvider(connection);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var seedUser = new ApplicationUser
        {
            UserName = "itdept",
            Email = "it@itportal.com",
            FullName = "IT Administrator",
            Department = "IT",
            EmailConfirmed = true,
            IsActive = true
        };
        var createResult = await userManager.CreateAsync(seedUser, "Configured123!");
        Assert.True(createResult.Succeeded);
        context.UserTitles.AddRange(
            new UserTitle { UserId = seedUser.Id, Title = PortalTitles.IT },
            new UserTitle { UserId = seedUser.Id, Title = PortalTitles.SuperAdmin });
        await context.SaveChangesAsync();
        var configuration = CreateConfiguration("owner@example.com", "Configured123!");

        await DbInitializer.SeedUsersAsync(userManager, context, configuration);

        var configuredUser = await context.Users.SingleAsync(user => user.Email == "owner@example.com");
        var superAdminUserIds = await context.UserTitles
            .Where(title => title.Title == PortalTitles.SuperAdmin)
            .Select(title => title.UserId)
            .ToListAsync();

        Assert.Equal([configuredUser.Id], superAdminUserIds);
    }

    private static ServiceProvider CreateServiceProvider(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.AddSingleton<ILicenseEncryptionService, PassThroughLicenseEncryptionService>();
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateConfiguration(string superAdminEmail, string seedPassword)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedSuperAdmin:Email"] = superAdminEmail,
                ["SeedAdmin:Password"] = seedPassword
            })
            .Build();

    private sealed class PassThroughLicenseEncryptionService : ILicenseEncryptionService
    {
        public string EncryptKey(string plainText) => plainText;

        public string DecryptKey(string cipherText) => cipherText;
    }
}
