// Project comment: QuotaReadOnlyBehaviorTests.cs verifies quota read-only write blocking.

using FluentValidation;
using IT_Form.Application.Common.Behaviors;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Application.Features.Departments.Writes.CreateDepartment;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public sealed class QuotaReadOnlyBehaviorTests
{
    [Fact]
    public async Task Handle_BlocksWriteCommand_WhenQuotaIsReadOnlyForItUser()
    {
        await using var context = CreateContext();
        await SetQuotaReadOnlyAsync(context, QuotaControl.Departments);
        var behavior = new QuotaReadOnlyBehavior<CreateDepartmentCommand, int>(
            new QuotaControl(context),
            new TestCurrentUserAccessor(titles: ["IT"]));

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateDepartmentCommand("Finance", null, true, [1]),
                () => Task.FromResult(1),
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.ErrorMessage == QuotaControl.ReadOnlyMessage);
    }

    [Fact]
    public async Task Handle_AllowsWriteCommand_WhenQuotaIsReadOnlyForSuperAdmin()
    {
        await using var context = CreateContext();
        await SetQuotaReadOnlyAsync(context, QuotaControl.Departments);
        var behavior = new QuotaReadOnlyBehavior<CreateDepartmentCommand, int>(
            new QuotaControl(context),
            new TestCurrentUserAccessor(titles: ["SuperAdmin"]));

        var result = await behavior.Handle(
            new CreateDepartmentCommand("Finance", null, true, [1]),
            () => Task.FromResult(42),
            CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Handle_AllowsReadRequest_WhenQuotaIsReadOnly()
    {
        await using var context = CreateContext();
        await SetQuotaReadOnlyAsync(context, QuotaControl.Departments);
        var behavior = new QuotaReadOnlyBehavior<ReadOnlyProbeQuery, int>(
            new QuotaControl(context),
            new TestCurrentUserAccessor(titles: ["IT"]));

        var result = await behavior.Handle(
            new ReadOnlyProbeQuery(),
            () => Task.FromResult(7),
            CancellationToken.None);

        Assert.Equal(7, result);
    }

    [Fact]
    public async Task GetSnapshots_ExcludesSuperAdmin_FromSystemUserUsage()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new ApplicationUser { Id = "active-user", UserName = "active.user", FullName = "Active User", IsActive = true },
            new ApplicationUser { Id = "super-admin", UserName = "super.admin", FullName = "Super Admin", IsActive = true },
            new ApplicationUser { Id = "inactive-user", UserName = "inactive.user", FullName = "Inactive User", IsActive = false });
        context.UserTitles.Add(new UserTitle { UserId = "super-admin", Title = "SuperAdmin" });
        await context.SaveChangesAsync();

        var snapshots = await new QuotaControl(context).GetSnapshotsAsync();

        var usersQuota = Assert.Single(snapshots, snapshot => snapshot.Key == QuotaControl.Users);
        Assert.Equal(1, usersQuota.CurrentUsage);
    }

    [Fact]
    public async Task Handle_BlocksWriteCommand_WhenMaintenanceModeIsEnabledForItUser()
    {
        await using var context = CreateContext();
        var maintenance = new MaintenanceModeService(context);
        await maintenance.SetStateAsync(true, "Planned maintenance", "super-admin", CancellationToken.None);
        var behavior = new QuotaReadOnlyBehavior<CreateDepartmentCommand, int>(
            new QuotaControl(context),
            new TestCurrentUserAccessor(titles: ["IT"]),
            maintenance);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateDepartmentCommand("Finance", null, true, [1]),
                () => Task.FromResult(1),
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.ErrorMessage.Contains("Maintenance mode is enabled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_AllowsWriteCommand_WhenMaintenanceModeIsEnabledForSuperAdmin()
    {
        await using var context = CreateContext();
        var maintenance = new MaintenanceModeService(context);
        await maintenance.SetStateAsync(true, "Planned maintenance", "super-admin", CancellationToken.None);
        var behavior = new QuotaReadOnlyBehavior<CreateDepartmentCommand, int>(
            new QuotaControl(context),
            new TestCurrentUserAccessor(titles: ["SuperAdmin"]),
            maintenance);

        var result = await behavior.Handle(
            new CreateDepartmentCommand("Finance", null, true, [1]),
            () => Task.FromResult(42),
            CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Handle_BlocksUnquotaWriteCommand_WhenMaintenanceModeIsEnabledForItUser()
    {
        await using var context = CreateContext();
        var maintenance = new MaintenanceModeService(context);
        await maintenance.SetStateAsync(true, "Planned maintenance", "super-admin", CancellationToken.None);
        var behavior = new QuotaReadOnlyBehavior<UnquotaProbeCommand, int>(
            new QuotaControl(context),
            new TestCurrentUserAccessor(titles: ["IT"]),
            maintenance);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new UnquotaProbeCommand(),
                () => Task.FromResult(1),
                CancellationToken.None));
    }

    private static async Task SetQuotaReadOnlyAsync(ApplicationDbContext context, string quotaKey)
    {
        var quota = new QuotaControl(context);
        await quota.EnsureDefaultsAsync();
        var setting = await context.SystemSettings.SingleAsync(item => item.Key == $"ControlPlane.Quota.{quotaKey}.ReadOnly");
        setting.Value = bool.TrueString;
        await context.SaveChangesAsync();
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

    private sealed record ReadOnlyProbeQuery() : IRequest<int>;
    private sealed record UnquotaProbeCommand() : IRequest<int>;
}
