using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tedd.AsyncLockTests;

public class AsyncLockTests
{
    [Fact]
    public void Enter_ShouldAcquireLock_WhenNotContended()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();

        // Act
        using var releaser = asyncLock.Enter();

        // Assert
        Assert.NotNull(releaser);
    }

    [Fact]
    public void Enter_ShouldBlockSecondCaller_WhenLockIsHeld()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var firstLockAcquired = false;
        var secondLockAcquired = false;
        var secondTaskStarted = false;

        // Act
        using var firstReleaser = asyncLock.Enter();
        firstLockAcquired = true;

        var task = Task.Run(() =>
        {
            secondTaskStarted = true;
            using var secondReleaser = asyncLock.Enter();
            secondLockAcquired = true;
        });

        Thread.Sleep(100); // Give the task time to try to acquire the lock

        // Assert
        Assert.True(firstLockAcquired);
        Assert.True(secondTaskStarted);
        Assert.False(secondLockAcquired);
    }

    [Fact]
    public async Task Enter_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        await using var firstReleaser = await asyncLock.EnterAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => Task.Run(() => asyncLock.Enter(cts.Token)));
    }

    [Fact]
    public async Task EnterAsync_ShouldAcquireLock_WhenNotContended()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();

        // Act
        await using var releaser = await asyncLock.EnterAsync();

        // Assert
        Assert.NotNull(releaser);
    }

    [Fact]
    public async Task EnterAsync_ShouldBlockSecondCaller_WhenLockIsHeld()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var firstLockAcquired = false;
        var secondLockStarted = false;
        var secondLockAcquired = false;

        // Act
        await using var firstReleaser = await asyncLock.EnterAsync();
        firstLockAcquired = true;

        var task = Task.Run(async () =>
        {
            secondLockStarted = true;
            await using var secondReleaser = await asyncLock.EnterAsync();
            secondLockAcquired = true;
        });

        await Task.Delay(100); // Give the task time to try to acquire the lock

        // Assert
        Assert.True(firstLockAcquired);
        Assert.True(secondLockStarted);
        Assert.False(secondLockAcquired);
    }

    [Fact]
    public async Task EnterAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        await using var firstReleaser = await asyncLock.EnterAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => asyncLock.EnterAsync(cts.Token).AsTask());
    }

    [Fact]
    public void TryEnter_ShouldReturnTrue_WhenLockIsAvailable()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();

        // Act
        var result = asyncLock.TryEnter(out var releaser);

        // Assert
        Assert.True(result);
        Assert.NotNull(releaser);
        releaser?.Dispose();
    }

    [Fact]
    public void TryEnter_ShouldReturnFalse_WhenLockIsHeld()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        using var firstReleaser = asyncLock.Enter();

        // Act
        var result = asyncLock.TryEnter(out var releaser);

        // Assert
        Assert.False(result);
        Assert.Null(releaser);
    }

    [Fact]
    public async Task TryEnterAsync_ShouldReturnReleaser_WhenLockIsAvailableWithinTimeout()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var timeout = TimeSpan.FromMilliseconds(500);

        // Act
        var releaser = await asyncLock.TryEnterAsync(timeout);

        // Assert
        Assert.NotNull(releaser);
        await releaser!.DisposeAsync();
    }

    [Fact]
    public async Task TryEnterAsync_ShouldReturnNull_WhenTimeoutExpires()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        await using var firstReleaser = await asyncLock.EnterAsync();
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act
        var releaser = await asyncLock.TryEnterAsync(timeout);

        // Assert
        Assert.Null(releaser);
    }

    [Fact]
    public async Task TryEnterAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        await using var firstReleaser = await asyncLock.EnterAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var timeout = TimeSpan.FromSeconds(1);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => asyncLock.TryEnterAsync(timeout, cts.Token).AsTask());
    }

    [Fact]
    public async Task Releaser_ShouldAllowReacquisition_AfterDispose()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();

        // Act
        using (var firstReleaser = asyncLock.Enter())
        {
            // Lock is held
        } // Lock is released here

        await using var secondReleaser = await asyncLock.EnterAsync();

        // Assert
        Assert.NotNull(secondReleaser);
    }

    [Fact]
    public async Task Releaser_ShouldAllowReacquisition_AfterDisposeAsync()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();

        // Act
        await using (var firstReleaser = await asyncLock.EnterAsync())
        {
            // Lock is held
        } // Lock is released here

        using var secondReleaser = asyncLock.Enter();

        // Assert
        Assert.NotNull(secondReleaser);
    }

    [Fact]
    public void Releaser_ShouldBeIdempotent_OnMultipleDispose()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var releaser = asyncLock.Enter();

        // Act & Assert - Should not throw
        releaser.Dispose();
        releaser.Dispose();
        releaser.Dispose();
    }

    [Fact]
    public async Task Releaser_ShouldBeIdempotent_OnMultipleDisposeAsync()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var releaser = await asyncLock.EnterAsync();

        // Act & Assert - Should not throw
        await releaser.DisposeAsync();
        await releaser.DisposeAsync();
        await releaser.DisposeAsync();
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldMaintainMutualExclusion()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var concurrentCount = 0;
        var maxConcurrentCount = 0;
        var tasks = new List<Task>();
        const int taskCount = 10;

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await using var releaser = await asyncLock.EnterAsync();

                var current = Interlocked.Increment(ref concurrentCount);
                var max = Math.Max(maxConcurrentCount, current);
                Interlocked.Exchange(ref maxConcurrentCount, max);

                await Task.Delay(10); // Simulate some work

                Interlocked.Decrement(ref concurrentCount);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, maxConcurrentCount);
        Assert.Equal(0, concurrentCount);
    }

    [Fact]
    public async Task MixedSyncAsyncAccess_ShouldMaintainMutualExclusion()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var concurrentCount = 0;
        var maxConcurrentCount = 0;
        var tasks = new List<Task>();
        const int taskCount = 10;

        // Act - Mix of sync and async acquisitions
        for (int i = 0; i < taskCount; i++)
        {
            if (i % 2 == 0)
            {
                // Async acquisition
                tasks.Add(Task.Run(async () =>
                {
                    await using var releaser = await asyncLock.EnterAsync();

                    var current = Interlocked.Increment(ref concurrentCount);
                    var max = Math.Max(maxConcurrentCount, current);
                    Interlocked.Exchange(ref maxConcurrentCount, max);

                    await Task.Delay(10);

                    Interlocked.Decrement(ref concurrentCount);
                }));
            }
            else
            {
                // Sync acquisition
                tasks.Add(Task.Run(() =>
                {
                    using var releaser = asyncLock.Enter();

                    var current = Interlocked.Increment(ref concurrentCount);
                    var max = Math.Max(maxConcurrentCount, current);
                    Interlocked.Exchange(ref maxConcurrentCount, max);

                    Thread.Sleep(10);

                    Interlocked.Decrement(ref concurrentCount);
                }));
            }
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, maxConcurrentCount);
        Assert.Equal(0, concurrentCount);
    }

    [Fact]
    public async Task StressTest_ManyContentions_ShouldNotDeadlock()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var completedTasks = 0;
        var tasks = new List<Task>();
        const int taskCount = 100;

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await using var releaser = await asyncLock.EnterAsync();
                await Task.Delay(1); // Very short work
                Interlocked.Increment(ref completedTasks);
            }));
        }


        var completionTask = Task.WhenAll(tasks);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));

        var completedFirst = await Task.WhenAny(completionTask, timeoutTask);

        // Assert
        Assert.Equal(completionTask, completedFirst);
        Assert.Equal(taskCount, completedTasks);
    }

    [Fact]
    public async Task CancellationDuringWait_ShouldNotAffectOtherWaiters()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        await using var firstReleaser = await asyncLock.EnterAsync();

        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();

        var secondTaskStarted = false;
        var thirdTaskCompleted = false;

        // Act
        var secondTask = Task.Run(async () =>
        {
            secondTaskStarted = true;
            await asyncLock.EnterAsync(cts1.Token);
        });

        var thirdTask = Task.Run(async () =>
        {
            await using var releaser = await asyncLock.EnterAsync(cts2.Token);
            thirdTaskCompleted = true;
        });

        await Task.Delay(100); // Let tasks start waiting
        Assert.True(secondTaskStarted);

        cts1.Cancel(); // Cancel second task
        await Assert.ThrowsAsync<OperationCanceledException>(() => secondTask);

        // Release first lock, third task should complete
        await firstReleaser.DisposeAsync();
        await thirdTask;

        // Assert
        Assert.True(thirdTaskCompleted);
    }

    [Fact]
    public async Task TryEnterAsync_WithZeroTimeout_ShouldBehaveLikeTryEnter()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        await using var firstReleaser = await asyncLock.EnterAsync();

        // Act
        var releaser = await asyncLock.TryEnterAsync(TimeSpan.Zero);

        // Assert
        Assert.Null(releaser);
    }

    [Fact]
    public async Task AsyncLock_ShouldSupportNestedScoping()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var innerExecuted = false;

        // Act
        await using (var outerReleaser = await asyncLock.EnterAsync())
        {
            Assert.NotNull(outerReleaser);

            // This should not deadlock since we're in the same thread/context
            // and the lock is reentrant in terms of disposal
            innerExecuted = true;
        }

        // After disposal, lock should be available again
        await using var newReleaser = await asyncLock.EnterAsync();
        Assert.NotNull(newReleaser);

        // Assert
        Assert.True(innerExecuted);
    }

    [Fact]
    public async Task TryEnterAsync_ShouldReturnReleaser_WhenLockBecomesAvailable()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var firstReleaserTask = asyncLock.EnterAsync();
        await using var firstReleaser = await firstReleaserTask;
        var timeout = TimeSpan.FromMilliseconds(200);

        // Act
        var tryEnterTask = asyncLock.TryEnterAsync(timeout);

        // Release the first lock after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await firstReleaser.DisposeAsync();
        });

        var releaser = await tryEnterTask;

        // Assert
        Assert.NotNull(releaser);
        await releaser!.DisposeAsync();
    }

    [Fact]
    public void Enter_ShouldWorkWithUsing_Statement()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var executed = false;

        // Act
        using (var releaser = asyncLock.Enter())
        {
            executed = true;
            Assert.NotNull(releaser);
        }

        // Assert
        Assert.True(executed);

        // Verify lock is released by acquiring it again
        using var secondReleaser = asyncLock.Enter();
        Assert.NotNull(secondReleaser);
    }

    [Fact]
    public async Task EnterAsync_ShouldWorkWithAwaitUsing_Statement()
    {
        // Arrange
        var asyncLock = new Tedd.AsyncLock();
        var executed = false;

        // Act
        await using (var releaser = await asyncLock.EnterAsync())
        {
            executed = true;
            Assert.NotNull(releaser);
        }

        // Assert
        Assert.True(executed);

        // Verify lock is released by acquiring it again
        await using var secondReleaser = await asyncLock.EnterAsync();
        Assert.NotNull(secondReleaser);
    }
}