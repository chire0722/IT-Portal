// Project comment: AuditLoggingBehaviorTests.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Behaviors;
using IT_Form.Data;
using IT_Form.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IT_Form.AuditTests;

/// <summary>
/// Defines the AuditLoggingBehaviorTests class used by the IT Form application.
/// </summary>
public class AuditLoggingBehaviorTests
{
    /// <summary>
    /// Carries data needed to execute the SampleSuccess command.
    /// </summary>
    public sealed record SampleSuccessCommand(string Name);
    /// <summary>
    /// Carries data needed to execute the SampleFailure command.
    /// </summary>
    public sealed record SampleFailureCommand(string Name);

    [Fact]
    /// <summary>
    /// Runs the Handle_Success_WritesCommandAndEntityAudits_WithSharedCorrelationId operation.
    /// </summary>
    public async Task Handle_Success_WritesCommandAndEntityAudits_WithSharedCorrelationId()
    {
        var (context, httpContext) = TestContextFactory.Create();
        var behavior = new AuditLoggingBehavior<SampleSuccessCommand, string>(
            context,
            new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext },
            null,
            NullLogger<AuditLoggingBehavior<SampleSuccessCommand, string>>.Instance);

        var result = await behavior.Handle(
            new SampleSuccessCommand("Finance"),
            async () =>
            {
                context.Departments.Add(new Department { Name = "Finance" });
                await context.SaveChangesAsync();
                return "ok";
            },
            CancellationToken.None);

        Assert.Equal("ok", result);

        var logs = context.AuditLogs.OrderBy(x => x.CreatedAt).ToList();
        Assert.Equal(2, logs.Count);

        var entityLog = Assert.Single(logs, x => x.EntityName == nameof(Department));
        var commandLog = Assert.Single(logs, x => x.EntityName == "Command");

        Assert.True(entityLog.CorrelationId.HasValue);
        Assert.Equal(entityLog.CorrelationId, commandLog.CorrelationId);
        Assert.True(commandLog.IsSuccess);
        Assert.Equal("auditor@ytc.com", commandLog.UserId);
    }

    [Fact]
    /// <summary>
    /// Runs the Handle_Success_DoesNotOpenSecondaryScopeForCommandAudit operation.
    /// </summary>
    public async Task Handle_Success_DoesNotOpenSecondaryScopeForCommandAudit()
    {
        var (context, httpContext) = TestContextFactory.Create();
        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var behavior = new AuditLoggingBehavior<SampleSuccessCommand, string>(
            context,
            new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext },
            scopeFactory.Object,
            NullLogger<AuditLoggingBehavior<SampleSuccessCommand, string>>.Instance);

        var result = await behavior.Handle(
            new SampleSuccessCommand("Ops"),
            async () =>
            {
                context.Departments.Add(new Department { Name = "Ops" });
                await context.SaveChangesAsync();
                return "ok";
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(2, context.AuditLogs.Count());
        scopeFactory.Verify(factory => factory.CreateScope(), Times.Never);
    }

    [Fact]
    /// <summary>
    /// Runs the Handle_Failure_WritesFailedCommandAudit_AndRethrows operation.
    /// </summary>
    public async Task Handle_Failure_WritesFailedCommandAudit_AndRethrows()
    {
        var (context, httpContext) = TestContextFactory.Create();
        var behavior = new AuditLoggingBehavior<SampleFailureCommand, string>(
            context,
            new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext },
            null,
            NullLogger<AuditLoggingBehavior<SampleFailureCommand, string>>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new SampleFailureCommand("broken"),
                () => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        Assert.Equal("boom", exception.Message);

        var log = Assert.Single(context.AuditLogs);
        Assert.Equal("Command", log.EntityName);
        Assert.False(log.IsSuccess);
        Assert.Equal("boom", log.ErrorMessage);
        Assert.True(log.CorrelationId.HasValue);
    }

    [Fact]
    /// <summary>
    /// Runs the Handle_ValidationFailure_WritesFailedCommandAudit_WithoutErrorLog operation.
    /// </summary>
    public async Task Handle_ValidationFailure_WritesFailedCommandAudit_WithoutErrorLog()
    {
        var (context, httpContext) = TestContextFactory.Create();
        var logger = new Mock<ILogger<AuditLoggingBehavior<SampleFailureCommand, string>>>();
        var behavior = new AuditLoggingBehavior<SampleFailureCommand, string>(
            context,
            new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext },
            null,
            logger.Object);
        var validationException = new ValidationException([
            new ValidationFailure("CurrentPassword", "Incorrect password.")
        ]);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new SampleFailureCommand("invalid"),
                () => throw validationException,
                CancellationToken.None));

        Assert.Same(validationException, exception);

        var log = Assert.Single(context.AuditLogs);
        Assert.Equal("Command", log.EntityName);
        Assert.False(log.IsSuccess);
        Assert.Contains("Incorrect password.", log.ErrorMessage, StringComparison.Ordinal);

        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString()!.StartsWith("Validation failed for command SampleFailureCommand", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
