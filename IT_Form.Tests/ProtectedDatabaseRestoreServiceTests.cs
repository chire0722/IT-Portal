// Project comment: ProtectedDatabaseRestoreServiceTests.cs verifies startup restore for protected database backups.

using IT_Form.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IT_Form.Tests;

public sealed class ProtectedDatabaseRestoreServiceTests
{
    private const string BackupPurpose = "IT_Form.DatabaseBackups.v1";

    [Fact]
    public async Task TryRestoreIfNeeded_RestoresLatestProtectedBackup_WhenDatabaseIsMissing()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var appDataPath = Path.Combine(rootPath, "App_Data");
            var backupPath = Path.Combine(appDataPath, "backups");
            Directory.CreateDirectory(backupPath);

            var keyPath = Path.Combine(rootPath, "keys");
            var provider = CreateProvider(keyPath);
            var sourcePath = await CreateSqliteDatabaseAsync(rootPath, "source.db", "Restored");
            var protectedPath = Path.Combine(backupPath, "IT_FormDB_20260505_010203.db.protected");
            await WriteProtectedBackupAsync(provider, sourcePath, protectedPath);

            var service = CreateService(rootPath, provider);
            var targetPath = Path.Combine(appDataPath, "IT_FormDB.db");

            var restored = service.TryRestoreIfNeeded(targetPath, appDataPath);

            Assert.True(restored);
            await AssertProbeValueAsync(targetPath, "Restored");
            Assert.True(File.Exists(protectedPath));
        }
        finally
        {
            DeleteTempRoot(rootPath);
        }
    }

    [Fact]
    public async Task TryRestoreIfNeeded_UsesAppDataKeyRing_WhenCurrentProviderCannotDecrypt()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var appDataPath = Path.Combine(rootPath, "App_Data");
            var backupPath = Path.Combine(appDataPath, "backups");
            Directory.CreateDirectory(backupPath);

            var appDataProvider = CreateProvider(Path.Combine(appDataPath, "DataProtection-Keys"));
            var currentProvider = CreateProvider(Path.Combine(rootPath, "different-keys"));
            var sourcePath = await CreateSqliteDatabaseAsync(rootPath, "source.db", "PublishedKeys");
            var protectedPath = Path.Combine(backupPath, "IT_FormDB_20260505_020304.db.protected");
            await WriteProtectedBackupAsync(appDataProvider, sourcePath, protectedPath);

            var service = CreateService(rootPath, currentProvider);
            var targetPath = Path.Combine(appDataPath, "IT_FormDB.db");

            var restored = service.TryRestoreIfNeeded(targetPath, appDataPath);

            Assert.True(restored);
            await AssertProbeValueAsync(targetPath, "PublishedKeys");
        }
        finally
        {
            DeleteTempRoot(rootPath);
        }
    }

    [Fact]
    public async Task TryRestoreIfNeeded_ReplacesRenamedProtectedDatabase_AndDeletesSidecars()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var appDataPath = Path.Combine(rootPath, "App_Data");
            Directory.CreateDirectory(appDataPath);

            var provider = CreateProvider(Path.Combine(rootPath, "keys"));
            var sourcePath = await CreateSqliteDatabaseAsync(rootPath, "source.db", "Renamed");
            var targetPath = Path.Combine(appDataPath, "IT_FormDB.db");
            await WriteProtectedBackupAsync(provider, sourcePath, targetPath);
            await File.WriteAllTextAsync(targetPath + "-wal", "stale wal");
            await File.WriteAllTextAsync(targetPath + "-shm", "stale shm");

            var service = CreateService(rootPath, provider);

            var restored = service.TryRestoreIfNeeded(targetPath, appDataPath);

            Assert.True(restored);
            await AssertProbeValueAsync(targetPath, "Renamed");
            Assert.Empty(Directory.GetFiles(appDataPath, "IT_FormDB.db-wal"));
            Assert.Empty(Directory.GetFiles(appDataPath, "IT_FormDB.db-shm"));
            Assert.Single(Directory.GetFiles(appDataPath, "IT_FormDB.db.invalid-*.bak"));
        }
        finally
        {
            DeleteTempRoot(rootPath);
        }
    }

    private static ProtectedDatabaseRestoreService CreateService(string rootPath, IDataProtectionProvider provider)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.ContentRootPath).Returns(rootPath);

        return new ProtectedDatabaseRestoreService(
            provider,
            environment.Object,
            NullLogger<ProtectedDatabaseRestoreService>.Instance);
    }

    private static IDataProtectionProvider CreateProvider(string keyPath)
    {
        Directory.CreateDirectory(keyPath);
        return DataProtectionProvider.Create(
            new DirectoryInfo(keyPath),
            builder => builder.SetApplicationName("IT_Form"));
    }

    private static async Task WriteProtectedBackupAsync(
        IDataProtectionProvider provider,
        string sourcePath,
        string protectedPath)
    {
        var protector = provider.CreateProtector(BackupPurpose);
        var sourceBytes = await ReadAllBytesWithSqliteSharingAsync(sourcePath);
        await File.WriteAllBytesAsync(protectedPath, protector.Protect(sourceBytes));
    }

    private static async Task<byte[]> ReadAllBytesWithSqliteSharingAsync(string sourcePath)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true);
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory);
        return memory.ToArray();
    }

    private static async Task<string> CreateSqliteDatabaseAsync(string rootPath, string fileName, string value)
    {
        var dbPath = Path.Combine(rootPath, fileName);
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = dbPath
        }.ToString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE RestoreProbe (
                Id INTEGER PRIMARY KEY,
                Value TEXT NOT NULL
            );
            INSERT INTO RestoreProbe (Value) VALUES ('{value}');
            """;
        await command.ExecuteNonQueryAsync();

        return dbPath;
    }

    private static async Task AssertProbeValueAsync(string dbPath, string expectedValue)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM RestoreProbe WHERE Id = 1;";
        Assert.Equal(expectedValue, await command.ExecuteScalarAsync());
    }

    private static string CreateTempRoot()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "IT_Form_ProtectedRestoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        return rootPath;
    }

    private static void DeleteTempRoot(string rootPath)
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
