namespace IT_Form.Application.Common.Abstractions;

public interface IKeyedLockProvider
{
    Task ExecuteAsync(
        string key,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
