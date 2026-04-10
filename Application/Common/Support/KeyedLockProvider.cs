using IT_Form.Application.Common.Abstractions;

namespace IT_Form.Application.Common.Support;

public sealed class KeyedLockProvider : IKeyedLockProvider
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, LockEntry> _entries = new(StringComparer.Ordinal);

    public Task ExecuteAsync(
        string key,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        => ExecuteAsync<object?>(
            key,
            async ct =>
            {
                await action(ct);
                return null;
            },
            cancellationToken);

    public async Task<T> ExecuteAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(action);

        var entry = AcquireEntry(key);

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            ReleaseEntry(key, entry);
            throw;
        }

        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            entry.Semaphore.Release();
            ReleaseEntry(key, entry);
        }
    }

    private LockEntry AcquireEntry(string key)
    {
        lock (_syncRoot)
        {
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new LockEntry();
                _entries[key] = entry;
            }

            entry.ReferenceCount++;
            return entry;
        }
    }

    private void ReleaseEntry(string key, LockEntry entry)
    {
        SemaphoreSlim? semaphoreToDispose = null;

        lock (_syncRoot)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0 && _entries.Remove(key))
            {
                semaphoreToDispose = entry.Semaphore;
            }
        }

        semaphoreToDispose?.Dispose();
    }

    private sealed class LockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
    }
}
