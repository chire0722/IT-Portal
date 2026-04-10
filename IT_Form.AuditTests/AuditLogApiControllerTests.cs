using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.AuditLogs.Flows;
using IT_Form.Controllers;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IT_Form.AuditTests;

public class AuditLogApiControllerTests
{
    public AuditLogApiControllerTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task GetChain_FailuresOnly_ReturnsFilteredItemsWithClassification()
    {
        var (context, _) = TestContextFactory.Create();
        var correlationId = Guid.NewGuid();

        var commandLog = new AuditLog
        {
            Action = "SubmitOnboardingStep5FlowCommand",
            EntityName = "Command",
            EntityId = "-",
            UserId = "it@ytc.com",
            PerformedBy = "it@ytc.com",
            IsSuccess = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            Timestamp = DateTime.UtcNow.AddMinutes(-3),
            CorrelationId = correlationId
        };
        var failedEntityLog = new AuditLog
        {
            Action = "Updated",
            EntityName = nameof(Department),
            EntityId = "4",
            UserId = "it@ytc.com",
            PerformedBy = "it@ytc.com",
            IsSuccess = false,
            ErrorMessage = "db timeout",
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            Timestamp = DateTime.UtcNow.AddMinutes(-2),
            CorrelationId = correlationId
        };
        var securityLog = new AuditLog
        {
            Action = "Login",
            EntityName = "User Account",
            EntityId = "user-1",
            UserId = "it@ytc.com",
            PerformedBy = "it@ytc.com",
            IsSuccess = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            Timestamp = DateTime.UtcNow.AddMinutes(-1),
            CorrelationId = correlationId
        };

        context.AuditLogs.AddRange(commandLog, failedEntityLog, securityLog);
        await context.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(
                It.IsAny<GetAuditLogChainApiQuery>(),
                It.IsAny<CancellationToken>()))
            .Returns((GetAuditLogChainApiQuery query, CancellationToken token) =>
                new GetAuditLogChainApiQueryHandler(context).Handle(query, token));

        var controller = new AuditLogApiController(sender.Object);
        var result = await controller.GetChain(commandLog.Id, failuresOnly: true);

        var ok = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, ok.StatusCode ?? 200);
        var payload = Assert.IsType<AuditLogChainApiResponse>(ok.Value);

        Assert.True(payload.FailuresOnly);
        Assert.Equal(3, payload.TotalCount);
        Assert.Equal(1, payload.FailureCount);
        Assert.Equal(2, payload.Items.Count);

        var anchor = Assert.Single(payload.Items, x => x.Id == commandLog.Id);
        Assert.Equal("Command Pipeline", anchor.SourceType);
        Assert.Equal("Business Command", anchor.StageLabel);

        var failed = Assert.Single(payload.Items, x => x.Id == failedEntityLog.Id);
        Assert.Equal("Entity Change", failed.SourceType);
        Assert.Equal("Persistence", failed.StageLabel);
    }

    [Fact]
    public async Task GetSummary_ReturnsFailureBreakdowns_ForCurrentFilterScope()
    {
        var (context, _) = TestContextFactory.Create();
        var now = DateTime.UtcNow;

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Action = "SubmitOffboardingCreateCommand",
                EntityName = "Command",
                EntityId = "-",
                UserId = "hr1@ytc.com",
                PerformedBy = "hr1@ytc.com",
                IsSuccess = false,
                ErrorMessage = "invalid step",
                CreatedAt = now.AddDays(-1),
                Timestamp = now.AddDays(-1)
            },
            new AuditLog
            {
                Action = "SubmitOffboardingCreateCommand",
                EntityName = "Command",
                EntityId = "-",
                UserId = "hr1@ytc.com",
                PerformedBy = "hr1@ytc.com",
                IsSuccess = false,
                ErrorMessage = "invalid step",
                CreatedAt = now.AddHours(-2),
                Timestamp = now.AddHours(-2)
            },
            new AuditLog
            {
                Action = "SubmitOnboardingStep5FlowCommand",
                EntityName = "Command",
                EntityId = "-",
                UserId = "it1@ytc.com",
                PerformedBy = "it1@ytc.com",
                IsSuccess = true,
                CreatedAt = now.AddHours(-1),
                Timestamp = now.AddHours(-1)
            });

        await context.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(
                It.IsAny<GetAuditLogSummaryApiQuery>(),
                It.IsAny<CancellationToken>()))
            .Returns((GetAuditLogSummaryApiQuery query, CancellationToken token) =>
                new GetAuditLogSummaryApiQueryHandler(context).Handle(query, token));

        var controller = new AuditLogApiController(sender.Object);
        var result = await controller.GetSummary(entity: "Command", auditAction: null, search: null, startDate: null, endDate: null, isSuccess: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuditLogSummaryApiResponse>(ok.Value);

        Assert.Equal(3, payload.TotalCount);
        Assert.Equal(2, payload.FailureCount);
        Assert.Equal(3, payload.CommandCount);
        Assert.Equal(2, payload.FailedCommandCount);
        Assert.Equal("SubmitOffboardingCreateCommand", Assert.Single(payload.TopFailedActions).Label);
        Assert.Equal("hr1@ytc.com", Assert.Single(payload.TopFailedActors).Label);
        Assert.NotEmpty(payload.FailureTrend);
    }
}
