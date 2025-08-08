#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tedd;


/// <summary>
/// An async-compatible mutual exclusion lock with sync/async acquire paths.
/// Supports both synchronous and asynchronous usage patterns, including cancellation.
/// </summary>
/// <example>
/// await using var _ = await lock.EnterAsync();  // async
/// using var _ = lock.Enter();                   // sync
/// Or with scope:
/// await using (var _ = await lock.EnterAsync()) { /* ... */ }  // async
/// using (var _ = lock.Enter()) { /* ... */ };                  // sync
/// </example>
public sealed class AsyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Synchronously acquire the lock (honors cancellation).</summary>
    public Releaser Enter(CancellationToken cancellationToken = default)
    {
        _semaphore.Wait(cancellationToken);
        return new Releaser(_semaphore);
    }

    /// <summary>Try to acquire the lock without waiting.</summary>
    public bool TryEnter(out Releaser? releaser)
    {
        if (_semaphore.Wait(0))
        {
            releaser = new Releaser(_semaphore);
            return true;
        }
        releaser = null;
        return false;
    }

    /// <summary>Asynchronously acquire the lock (honors cancellation).</summary>
    public async ValueTask<Releaser> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(_semaphore);
    }

    /// <summary>Asynchronously try to acquire the lock, waiting up to <paramref name="timeout"/>.</summary>
    public async ValueTask<Releaser?> TryEnterAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
            return new Releaser(_semaphore);
        return null;
    }

    /// <summary>
    /// Token returned by Enter/EnterAsync. Disposing releases the lock.
    /// Implements both IDisposable and IAsyncDisposable; call the matching dispose
    /// for how you acquired the lock.
    /// </summary>
    public sealed class Releaser : IDisposable, IAsyncDisposable
    {
        private SemaphoreSlim? _toRelease;
        private int _released; // 0 = held, 1 = released

        internal Releaser(SemaphoreSlim toRelease) => _toRelease = toRelease;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                _toRelease?.Release();
            _toRelease = null;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
