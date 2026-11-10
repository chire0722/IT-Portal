using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace IT_Form.Data;

/// <summary>
/// Provides a deterministic design-time context for EF migrations without starting the web host.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=it-form-design-time.db")
            .Options;

        return new ApplicationDbContext(
            options,
            new HttpContextAccessor(),
            NullLogger<ApplicationDbContext>.Instance,
            new LicenseEncryptionService(configuration),
            new ServiceCollection().BuildServiceProvider());
    }
}
