// Project comment: DataRetentionServiceTests.cs is part of the IT Form application source code.

using System.Reflection;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace IT_Form.Tests;

public sealed class DataRetentionServiceTests
{
    [Fact]
    public async Task RetentionPass_RemovesExpiredTerminalEmailOutboxButKeepsPendingMessages()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>())).Returns((string value) => value);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>())).Returns((string value) => value);

        var services = new ServiceCollection();
        services.AddScoped(_ => new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options,
            Mock.Of<IHttpContextAccessor>(),
            NullLogger<ApplicationDbContext>.Instance,
            encryption.Object));
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.EmailOutboxMessages.AddRange(
                new EmailOutboxMessage
                {
                    MessageType = nameof(OtpEmailMessage),
                    PayloadJson = "{\"Email\":\"old@example.com\"}",
                    Status = EmailOutboxStatus.Sent,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                },
                new EmailOutboxMessage
                {
                    MessageType = nameof(OtpEmailMessage),
                    PayloadJson = "{\"Email\":\"pending@example.com\"}",
                    Status = EmailOutboxStatus.Pending,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                });
            await db.SaveChangesAsync();
        }

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.ContentRootPath).Returns(Path.GetTempPath());
        environment.SetupGet(item => item.WebRootPath).Returns(Path.GetTempPath());
        var options = Options.Create(new PdpaComplianceOptions
        {
            RecordRetentionDays = 1,
            SecurityTokenRetentionDays = 30,
            AuditLogRetentionDays = 1
        });
        var service = new DataRetentionService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DataRetentionService>.Instance,
            environment.Object,
            options);
        var runRetentionPass = typeof(DataRetentionService)
            .GetMethod("RunRetentionPass", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await (Task)runRetentionPass.Invoke(service, [CancellationToken.None])!;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var remaining = await db.EmailOutboxMessages.AsNoTracking().ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(EmailOutboxStatus.Pending, remaining[0].Status);
        }
    }
}

