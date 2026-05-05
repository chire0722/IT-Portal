// Project comment: SafeSystemSettingsServiceTests.cs verifies safe control-plane setting persistence.

using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;
using IT_Form.Data;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IT_Form.Tests;

public sealed class SafeSystemSettingsServiceTests
{
    [Fact]
    public async Task SaveAsync_NormalizesNullStringSettings_BeforeWritingToSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var service = new SafeSystemSettingsService(context);

        await service.SaveAsync(
            new SafeSystemSettingsModel(null, null, 0, 0, 0, 0, null),
            "tester",
            CancellationToken.None);

        var settings = await context.SystemSettings
            .AsNoTracking()
            .ToDictionaryAsync(item => item.Key, item => item.Value);

        Assert.Equal(string.Empty, settings[ControlPlaneSettingKeys.SupportEmail]);
        Assert.Equal(string.Empty, settings[ControlPlaneSettingKeys.AuditNotificationEmail]);
        Assert.Equal("1", settings[ControlPlaneSettingKeys.RetentionRecordDays]);
        Assert.Equal("1", settings[ControlPlaneSettingKeys.RetentionTokenDays]);
        Assert.Equal("1", settings[ControlPlaneSettingKeys.RetentionAuditDays]);
        Assert.Equal("1", settings[ControlPlaneSettingKeys.OtpExpiryMinutes]);
        Assert.Equal("5,15,30,120", settings[ControlPlaneSettingKeys.LockoutSchedule]);
    }

    private static ApplicationDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
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
}
