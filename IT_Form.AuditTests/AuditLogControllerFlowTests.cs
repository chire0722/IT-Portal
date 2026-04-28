// Project comment: AuditLogControllerFlowTests.cs is part of the IT Form application source code.

using IT_Form.Controllers;
using IT_Form.Models;
using IT_Form.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.AuditTests;

/// <summary>
/// Defines the AuditLogControllerFlowTests class used by the IT Form application.
/// </summary>
public class AuditLogControllerFlowTests
{
    [Fact]
    /// <summary>
    /// Runs the Details_FailuresOnly_KeepsAnchorAndOnlyFailedChainEntries operation.
    /// </summary>
    public async Task Details_FailuresOnly_KeepsAnchorAndOnlyFailedChainEntries()
    {
        var (context, _) = TestContextFactory.Create();
        var correlationId = Guid.NewGuid();

        var anchor = new AuditLog
        {
            Action = "SubmitOffboardingCreateCommand",
            EntityName = "Command",
            EntityId = "-",
            UserId = "user@ytc.com",
            PerformedBy = "user@ytc.com",
            IsSuccess = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            Timestamp = DateTime.UtcNow.AddMinutes(-3),
            CorrelationId = correlationId
        };
        var failure = new AuditLog
        {
            Action = "Created",
            EntityName = nameof(Department),
            EntityId = "1",
            UserId = "user@ytc.com",
            PerformedBy = "user@ytc.com",
            IsSuccess = false,
            ErrorMessage = "write failed",
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            Timestamp = DateTime.UtcNow.AddMinutes(-2),
            CorrelationId = correlationId
        };
        var success = new AuditLog
        {
            Action = "Updated",
            EntityName = nameof(Department),
            EntityId = "1",
            UserId = "user@ytc.com",
            PerformedBy = "user@ytc.com",
            IsSuccess = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            Timestamp = DateTime.UtcNow.AddMinutes(-1),
            CorrelationId = correlationId
        };

        context.AuditLogs.AddRange(anchor, failure, success);
        await context.SaveChangesAsync();

        var controller = new AuditLogController(context);
        var result = await controller.Details(anchor.Id, failuresOnly: true);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AuditLogDetailsPageViewModel>(view.Model);

        Assert.True(model.FailuresOnly);
        Assert.Equal(3, model.ChainTotalCount);
        Assert.Equal(1, model.ChainFailureCount);
        Assert.Equal(2, model.VisibleChainCount);
        Assert.Contains(model.RelatedEntries, x => x.Id == anchor.Id);
        Assert.Contains(model.RelatedEntries, x => x.Id == failure.Id);
        Assert.DoesNotContain(model.RelatedEntries, x => x.Id == success.Id);
    }
}
