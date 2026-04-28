// Project comment: AuditLogApiFlowTests.cs is part of the IT Form application source code.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.AuditLogs.Flows;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the AuditLogApiFlowTests class used by the IT Form application.
/// </summary>
public class AuditLogApiFlowTests
{
    public AuditLogApiFlowTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetList_ReturnsProjectedItemsAndPagination operation.
    /// </summary>
    public async Task GetList_ReturnsProjectedItemsAndPagination()
    {
        await using var context = CreateContext();
        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 1,
                EntityName = "OnboardingRequest",
                EntityId = "100",
                Action = "Created",
                UserId = "user-a",
                PerformedBy = "user-a@company.com",
                CreatedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                IsSuccess = true
            },
            new AuditLog
            {
                Id = 2,
                EntityName = "OnboardingRequest",
                EntityId = "101",
                Action = "Updated",
                UserId = "user-b",
                PerformedBy = "user-b@company.com",
                CreatedAt = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                IsSuccess = false,
                ErrorMessage = "validation failed"
            },
            new AuditLog
            {
                Id = 3,
                EntityName = "OffboardingRequest",
                EntityId = "102",
                Action = "Deleted",
                UserId = "user-c",
                PerformedBy = "user-c@company.com",
                CreatedAt = new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc),
                IsSuccess = true
            });
        await context.SaveChangesAsync();

        var handler = new GetAuditLogListApiQueryHandler(context);

        var result = await handler.Handle(
            new GetAuditLogListApiQuery("OnboardingRequest", null, null, null, null, null, 1),
            CancellationToken.None);

        Assert.Equal(2, result.Pagination.TotalCount);
        Assert.Equal(1, result.Pagination.PageIndex);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Items.First().Id);
        Assert.Equal("validation failed", result.Items.First().ErrorMessage);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetDetails_MapsDerivedFields operation.
    /// </summary>
    public async Task GetDetails_MapsDerivedFields()
    {
        await using var context = CreateContext();
        var log = new AuditLog
        {
            Id = 7,
            EntityName = "Command",
            EntityId = "77",
            Action = "Created",
            UserId = "maker",
            PerformedBy = "maker@company.com",
            CreatedAt = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc),
            Timestamp = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc),
            IsSuccess = true,
            Details = "created record"
        };
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();

        var handler = new GetAuditLogDetailsApiQueryHandler(context);

        var result = await handler.Handle(new GetAuditLogDetailsApiQuery(7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(AuditLogClassifier.GetSourceType(log), result.Data!.SourceType);
        Assert.Equal(AuditLogClassifier.GetStageLabel(log), result.Data.StageLabel);
        Assert.Equal("created record", result.Data.Details);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetChain_FailuresOnly_KeepsAnchorAndFailures operation.
    /// </summary>
    public async Task GetChain_FailuresOnly_KeepsAnchorAndFailures()
    {
        await using var context = CreateContext();
        var correlationId = Guid.NewGuid();
        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 11,
                CorrelationId = correlationId,
                EntityName = "Command",
                EntityId = "11",
                Action = "Started",
                UserId = "user",
                PerformedBy = "user@company.com",
                CreatedAt = new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc),
                IsSuccess = true
            },
            new AuditLog
            {
                Id = 12,
                CorrelationId = correlationId,
                EntityName = "Command",
                EntityId = "11",
                Action = "Failed",
                UserId = "user",
                PerformedBy = "user@company.com",
                CreatedAt = new DateTime(2026, 4, 5, 10, 1, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 5, 10, 1, 0, DateTimeKind.Utc),
                IsSuccess = false,
                ErrorMessage = "boom"
            },
            new AuditLog
            {
                Id = 13,
                CorrelationId = correlationId,
                EntityName = "Command",
                EntityId = "11",
                Action = "Completed",
                UserId = "user",
                PerformedBy = "user@company.com",
                CreatedAt = new DateTime(2026, 4, 5, 10, 2, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 5, 10, 2, 0, DateTimeKind.Utc),
                IsSuccess = true
            });
        await context.SaveChangesAsync();

        var handler = new GetAuditLogChainApiQueryHandler(context);

        var result = await handler.Handle(new GetAuditLogChainApiQuery(11, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data!.TotalCount);
        Assert.Equal(1, result.Data.FailureCount);
        Assert.Equal(new[] { 11, 12 }, result.Data.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetSummary_ReturnsAggregatedCounts operation.
    /// </summary>
    public async Task GetSummary_ReturnsAggregatedCounts()
    {
        await using var context = CreateContext();
        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 21,
                EntityName = "Command",
                EntityId = "201",
                Action = "Approve",
                UserId = "user-1",
                PerformedBy = "alpha@company.com",
                CreatedAt = new DateTime(2026, 4, 6, 10, 0, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 6, 10, 0, 0, DateTimeKind.Utc),
                IsSuccess = false
            },
            new AuditLog
            {
                Id = 22,
                EntityName = "Command",
                EntityId = "202",
                Action = "Approve",
                UserId = "user-2",
                PerformedBy = "alpha@company.com",
                CreatedAt = new DateTime(2026, 4, 6, 11, 0, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 6, 11, 0, 0, DateTimeKind.Utc),
                IsSuccess = false
            },
            new AuditLog
            {
                Id = 23,
                EntityName = "Query",
                EntityId = "203",
                Action = "Read",
                UserId = "user-3",
                PerformedBy = "beta@company.com",
                CreatedAt = new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Utc),
                Timestamp = new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Utc),
                IsSuccess = true
            });
        await context.SaveChangesAsync();

        var handler = new GetAuditLogSummaryApiQueryHandler(context);

        var result = await handler.Handle(
            new GetAuditLogSummaryApiQuery(null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(2, result.FailureCount);
        Assert.Equal(2, result.CommandCount);
        Assert.Equal(2, result.FailedCommandCount);
        Assert.Equal("Approve", Assert.Single(result.TopFailedActions).Label);
        Assert.Equal("alpha@company.com", Assert.Single(result.TopFailedActors).Label);
        Assert.Single(result.FailureTrend);
    }

    /// <summary>
    /// Creates data for the CreateContext operation.
    /// </summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var http = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        encryption.Setup(x => x.DecryptKey(It.IsAny<string>())).Returns((string s) => s);

        return new ApplicationDbContext(options, http.Object, logger.Object, encryption.Object);
    }
}
