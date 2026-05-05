// Project comment: BackupManagementServiceTests.cs verifies control-plane backup behavior.

using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public sealed class BackupManagementServiceTests
{
    [Fact]
    public async Task RunManualBackupAsync_CreatesSnapshot_WhenDatabaseFileIsOpenForWriting()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "IT_Form_BackupTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);

        try
        {
            var appDataPath = Path.Combine(rootPath, "App_Data");
            var backupPath = Path.Combine(appDataPath, "backups");
            Directory.CreateDirectory(appDataPath);

            var dbPath = Path.Combine(appDataPath, "IT_FormDB.db");
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath
            }.ToString();

            await using var context = CreateContext(connectionString);
            await context.Database.EnsureCreatedAsync();
            context.SystemSettings.Add(new SystemSetting
            {
                Key = "BackupTest",
                Value = "Before",
                Description = "Seed data for backup verification."
            });
            await context.SaveChangesAsync();

            await using var liveConnection = new SqliteConnection(connectionString);
            await liveConnection.OpenAsync();
            using var openWriterHandle = new FileStream(dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            var dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(rootPath, "keys")));
            var service = new BackupManagementService(
                context,
                CreateConfiguration(backupPath),
                CreateEnvironment(rootPath),
                dataProtectionProvider);

            var summary = await service.RunManualBackupAsync("tester", CancellationToken.None);

            Assert.True(File.Exists(summary.FullPath));
            Assert.EndsWith(".db.protected", summary.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.GetFiles(Path.Combine(backupPath, "temp")));

            var protectedBytes = await File.ReadAllBytesAsync(summary.FullPath);
            var snapshotBytes = dataProtectionProvider
                .CreateProtector("IT_Form.DatabaseBackups.v1")
                .Unprotect(protectedBytes);

            var snapshotPath = Path.Combine(rootPath, "snapshot.db");
            await File.WriteAllBytesAsync(snapshotPath, snapshotBytes);

            await using var snapshotConnection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = snapshotPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString());
            await snapshotConnection.OpenAsync();

            await using var command = snapshotConnection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SystemSettings WHERE Key = 'BackupTest';";

            var count = Assert.IsType<long>(await command.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }
        finally
        {
            try
            {
                Directory.Delete(rootPath, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for test temp files.
            }
        }
    }

    private static ApplicationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>()))
            .Returns((string value) => value);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>()))
            .Returns((string value) => value);

        return new ApplicationDbContext(
            options,
            Mock.Of<IHttpContextAccessor>(),
            NullLogger<ApplicationDbContext>.Instance,
            encryption.Object,
            new ServiceCollection().BuildServiceProvider());
    }

    private static IConfiguration CreateConfiguration(string backupPath)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseBackup:BackupPath"] = backupPath
            })
            .Build();

    private static IWebHostEnvironment CreateEnvironment(string rootPath)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.ContentRootPath).Returns(rootPath);
        environment.SetupGet(item => item.EnvironmentName).Returns("Development");
        return environment.Object;
    }
}
