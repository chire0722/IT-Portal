// Project comment: IKeyedLockProvider.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Abstractions;

/// <summary>
/// Defines the contract implemented by IKeyedLockProvider services.
/// </summary>
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
