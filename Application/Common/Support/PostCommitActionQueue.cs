// Project comment: PostCommitActionQueue.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Common.Support;

/// <summary>
/// Defines the PostCommitActionQueue class used by the IT Form application.
/// </summary>
public sealed class PostCommitActionQueue : IPostCommitActionQueue
{
    private readonly object _syncRoot = new();
    private readonly Queue<Func<CancellationToken, Task>> _actions = new();
    private readonly ILogger<PostCommitActionQueue> _logger;

    public PostCommitActionQueue(ILogger<PostCommitActionQueue> logger)
    {
        _logger = logger;
    }

    public bool HasPendingActions
    {
        get
        {
            lock (_syncRoot)
            {
                return _actions.Count > 0;
            }
        }
    }

    /// <summary>
    /// Runs the Enqueue operation.
    /// </summary>
    public void Enqueue(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_syncRoot)
        {
            _actions.Enqueue(action);
        }
    }

    /// <summary>
    /// Runs the FlushAsync operation.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        while (TryDequeue(out var action))
        {
            try
            {
                await action!(cancellationToken);
            }
            catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
            {
                _logger.LogError(ex, "A post-commit action failed after the transaction was committed.");
            }
        }
    }

    /// <summary>
    /// Runs the Clear operation.
    /// </summary>
    public void Clear()
    {
        lock (_syncRoot)
        {
            _actions.Clear();
        }
    }

    /// <summary>
    /// Runs the TryDequeue operation.
    /// </summary>
    private bool TryDequeue(out Func<CancellationToken, Task>? action)
    {
        lock (_syncRoot)
        {
            if (_actions.Count == 0)
            {
                action = null;
                return false;
            }

            action = _actions.Dequeue();
            return true;
        }
    }
}

/// <summary>
/// Defines the NoOpPostCommitActionQueue class used by the IT Form application.
/// </summary>
internal sealed class NoOpPostCommitActionQueue : IPostCommitActionQueue
{
    public static NoOpPostCommitActionQueue Instance { get; } = new();

    public bool HasPendingActions => false;

    /// <summary>
    /// Runs the Enqueue operation.
    /// </summary>
    public void Enqueue(Func<CancellationToken, Task> action)
    {
    }

    /// <summary>
    /// Runs the FlushAsync operation.
    /// </summary>
    public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Runs the Clear operation.
    /// </summary>
    public void Clear()
    {
    }
}
