namespace IT_Form.Application.Common.Abstractions;

public interface IPostCommitActionQueue
{
    bool HasPendingActions { get; }

    void Enqueue(Func<CancellationToken, Task> action);

    Task FlushAsync(CancellationToken cancellationToken = default);

    void Clear();
}
