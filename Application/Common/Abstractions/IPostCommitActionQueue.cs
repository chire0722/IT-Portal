// Project comment: IPostCommitActionQueue.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Abstractions;

/// <summary>
/// Defines the contract implemented by IPostCommitActionQueue services.
/// </summary>
public interface IPostCommitActionQueue
{
    bool HasPendingActions { get; }

    void Enqueue(Func<CancellationToken, Task> action);

    Task FlushAsync(CancellationToken cancellationToken = default);

    void Clear();
}
