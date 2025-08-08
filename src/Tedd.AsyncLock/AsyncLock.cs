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
public sealed class AsyncLock : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Synchronously acquire the lock (honors cancellation).</summary>
    public Releaser Enter(CancellationToken cancellationToken = default)
    {
        _semaphore.Wait(cancellationToken);
        return Releaser.Rent(_semaphore);
    }

    /// <summary>Try to acquire the lock without waiting.</summary>
    public bool TryEnter(out Releaser? releaser)
    {
        if (_semaphore.Wait(0))
        {
            releaser = Releaser.Rent(_semaphore);
            return true;
        }
        releaser = null;
        return false;
    }

    /// <summary>Asynchronously acquire the lock (honors cancellation).</summary>
    public async ValueTask<Releaser> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return Releaser.Rent(_semaphore);
    }

    /// <summary>Asynchronously try to acquire the lock, waiting up to <paramref name="timeout"/>.</summary>
    public async ValueTask<Releaser?> TryEnterAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
            return Releaser.Rent(_semaphore);
        return null;
    }

    /// <summary>
    /// Token returned by Enter/EnterAsync. Disposing releases the lock and returns the token to a pool.
    /// Implements both IDisposable and IAsyncDisposable; call the matching dispose for how you acquired the lock.
    /// </summary>
    public sealed class Releaser : IDisposable, IAsyncDisposable
    {
        private SemaphoreSlim? _toRelease;
        private int _released; // 0 = held, 1 = released

        // Pool of Releasers with cleanup to reset internal fields.
        private static readonly ObjectPool<Releaser> Pool =
            new(
                factory: static () => new Releaser(),
                cleanup: static r => r.Cleanup(),
                size: Math.Max(4, Environment.ProcessorCount * 2));

        private Releaser() { } // pooled

        internal static Releaser Rent(SemaphoreSlim toRelease)
        {
            var r = Pool.Allocate();
            r._toRelease = toRelease;
            r._released = 0; // reset by pool cleanup as well; ensure fresh on rent
            return r;
        }

        public void Dispose()
        {
            // Only release and return to pool once.
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _toRelease?.Release();
                // Return this token to the pool; pool cleanup will clear fields.
                Pool.Free(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        // Called by pool on Free() before the instance is stored.
        private void Cleanup()
        {
            _toRelease = null;
            _released = 0;
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_semaphore is IAsyncDisposable semaphoreAsyncDisposable)
            await semaphoreAsyncDisposable.DisposeAsync();
        else
            _semaphore.Dispose();
    }
}
