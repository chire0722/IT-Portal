// Project comment: EmailDispatcherBackgroundServiceTests.cs is part of the IT Form application source code.

using System.Text.Json;
using System.Reflection;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the EmailDispatcherBackgroundServiceTests class used by the IT Form application.
/// </summary>
public class EmailDispatcherBackgroundServiceTests
{
    [Fact]
    public void GeneratedEmailMarkup_EncodesUserControlledValues()
    {
        var welcomeBody = (string)typeof(EmailDispatcherBackgroundService)
            .GetMethod("BuildWelcomeEmailBody", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, ["<img src=x onerror=alert(1)>"])!;
        var lockoutBody = (string)typeof(EmailDispatcherBackgroundService)
            .GetMethod("BuildLockoutAlertBody", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, ["attacker@example.com<script>", "user<1>", DateTime.UtcNow])!;

        Assert.Contains("&lt;img", welcomeBody);
        Assert.DoesNotContain("<img", welcomeBody);
        Assert.Contains("&lt;script&gt;", lockoutBody);
        Assert.Contains("user&lt;1&gt;", lockoutBody);
    }

    [Fact]
    /// <summary>
    /// Runs the ExecuteAsync_SendsOtpEmailViaSmtp operation.
    /// </summary>
    public async Task ExecuteAsync_SendsOtpEmailViaSmtp()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var deliverySignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outlookService = new Mock<IOutlookService>();
        outlookService
            .Setup(service => service.DisplayEmail("user@example.com", "OTP", "<b>ABC234</b>", null, null, "", null, false))
            .Callback(() => deliverySignal.TrySetResult());

        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>())).Returns((string input) => input);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>())).Returns((string input) => input);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(outlookService.Object)
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddSingleton<ILicenseEncryptionService>(encryption.Object)
            .AddScoped(_ => new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options,
                _.GetRequiredService<IHttpContextAccessor>(),
                NullLogger<ApplicationDbContext>.Instance,
                _.GetRequiredService<ILicenseEncryptionService>()))
            .BuildServiceProvider();

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                MessageType = nameof(OtpEmailMessage),
                PayloadJson = JsonSerializer.Serialize(
                    new OtpEmailMessage("user@example.com", "OTP", "<b>ABC234</b>"),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                Status = EmailOutboxStatus.Pending,
                AvailableAfterUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var worker = new EmailDispatcherBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EmailDispatcherBackgroundService>.Instance);

        await worker.StartAsync(CancellationToken.None);

        var completedTask = await Task.WhenAny(deliverySignal.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        await worker.StopAsync(CancellationToken.None);

        Assert.Same(deliverySignal.Task, completedTask);
        outlookService.Verify(
            service => service.DisplayEmail("user@example.com", "OTP", "<b>ABC234</b>", null, null, "", null, false),
            Times.Once);
    }
}

