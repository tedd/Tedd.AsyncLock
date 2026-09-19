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
    /// <remarks>Time Complexity: O(1) in the fast path. Space Complexity: O(1).</remarks>
    public Releaser Enter(CancellationToken cancellationToken = default)
    {
        _semaphore.Wait(cancellationToken);
        return Releaser.Rent(_semaphore);
    }

    /// <summary>Try to acquire the lock without waiting.</summary>
    /// <remarks>Time Complexity: O(1). Space Complexity: O(1).</remarks>
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
    /// <remarks>Time Complexity: O(1) in the fast path. Space Complexity: O(1).</remarks>
    public ValueTask<Releaser> EnterAsync(CancellationToken cancellationToken = default)
    {
        var waitTask = _semaphore.WaitAsync(cancellationToken);
        if (waitTask.Status == TaskStatus.RanToCompletion)
        {
            return new ValueTask<Releaser>(Releaser.Rent(_semaphore));
        }
        return new ValueTask<Releaser>(WaitAndRentAsync(waitTask));
    }

    private async Task<Releaser> WaitAndRentAsync(Task waitTask)
    {
        await waitTask.ConfigureAwait(false);
        return Releaser.Rent(_semaphore);
    }

    /// <summary>Asynchronously try to acquire the lock, waiting up to <paramref name="timeout"/>.</summary>
    /// <remarks>Time Complexity: O(1) in the fast path. Space Complexity: O(1).</remarks>
    public ValueTask<Releaser?> TryEnterAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var waitTask = _semaphore.WaitAsync(timeout, cancellationToken);
        if (waitTask.Status == TaskStatus.RanToCompletion)
        {
            // Note: Use `.Result` or `GetAwaiter().GetResult()` carefully here as .NET Framework
            // might not have IsCompletedSuccessfully. Since Status is RanToCompletion, Result is safe.
#pragma warning disable CA1849 // Synchronous wait is safe because task is completed
            var result = waitTask.Result;
#pragma warning restore CA1849
            return new ValueTask<Releaser?>(result ? Releaser.Rent(_semaphore) : null);
        }
        return new ValueTask<Releaser?>(WaitAndRentTryAsync(waitTask));
    }

    private async Task<Releaser?> WaitAndRentTryAsync(Task<bool> waitTask)
    {
        if (await waitTask.ConfigureAwait(false))
            return Releaser.Rent(_semaphore);
        return null;
    }

    /// <summary>
    /// Token returned by Enter/EnterAsync. Disposing releases the lock and returns the token to a pool.
    /// Implements both IDisposable and IAsyncDisposable; call the matching dispose for how you acquired the lock.
    /// </summary>
#pragma warning disable CA1034 // Nested type visibility
    public sealed class Releaser : IDisposable, IAsyncDisposable
#pragma warning restore CA1034
    {
        private SemaphoreSlim? _toRelease;

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
            return r;
        }

        /// <summary>Time Complexity: O(1). Space Complexity: O(1).</summary>
        public void Dispose()
        {
            // Only release and return to pool once.
            var semaphore = Interlocked.Exchange(ref _toRelease, null);
            if (semaphore != null)
            {
                semaphore.Release();
                // Return this token to the pool; pool cleanup will clear fields.
                Pool.Free(this);
            }
        }

        /// <summary>Time Complexity: O(1). Space Complexity: O(1).</summary>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        // Called by pool on Free() before the instance is stored.
        private void Cleanup()
        {
            _toRelease = null;
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_semaphore is IAsyncDisposable semaphoreAsyncDisposable)
            await semaphoreAsyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            _semaphore.Dispose();
    }
}
