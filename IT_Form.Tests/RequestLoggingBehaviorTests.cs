// Project comment: RequestLoggingBehaviorTests.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Behaviors;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Form.Tests;

/// <summary>
/// Defines the RequestLoggingBehaviorTests class used by the IT Form application.
/// </summary>
public sealed class RequestLoggingBehaviorTests
{
    [Fact]
    /// <summary>
    /// Runs the Handle_LogsCommandFailure_WithCommandName operation.
    /// </summary>
    public async Task Handle_LogsCommandFailure_WithCommandName()
    {
        var logger = new Mock<ILogger<RequestLoggingBehavior<FailingTestCommand, string>>>();
        var behavior = new RequestLoggingBehavior<FailingTestCommand, string>(logger.Object);
        var exception = new InvalidOperationException("boom");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new FailingTestCommand(),
                () => Task.FromException<string>(exception),
                CancellationToken.None));

        Assert.Same(exception, thrown);
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString() == "FAILED COMMAND FailingTestCommand"),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Carries data needed to execute the FailingTest command.
    /// </summary>
    public sealed record FailingTestCommand;
}
