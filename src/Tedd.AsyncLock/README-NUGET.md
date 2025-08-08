# Utilization Paradigms

## Asynchronous Acquisition

```csharp
using Tedd;

var mutex = new AsyncLock();

await using var guardian = await mutex.EnterAsync();
// Exclusive execution zone
await ExecuteAsynchronousOperation();
// Guardian disposal ensures lock release
```

## Synchronous Acquisition

```csharp
using Tedd;

var mutex = new AsyncLock();

using var guardian = mutex.Enter();
// Exclusive execution zone
ExecuteSynchronousOperation();
// Guardian disposal ensures lock release
```

## Scoped Constructs

```csharp
var mutex = new AsyncLock();

// Asynchronous scope
await using (var guardian = await mutex.EnterAsync())
{
    // Exclusive execution zone
    await ExecuteAsynchronousOperation();
}

// Synchronous scope
using (var guardian = mutex.Enter())
{
    // Exclusive execution zone
    ExecuteSynchronousOperation();
}
```

## Cancellation Semantics

```csharp
var mutex = new AsyncLock();
using var abortSource = new CancellationTokenSource();

try
{
    await using var guardian = await mutex.EnterAsync(abortSource.Token);
    await ExecuteOperation();
}
catch (OperationCanceledException)
{
    // Mitigate abortive state
}
```

## Non-Blocking Probe

```csharp
var mutex = new AsyncLock();

if (mutex.TryEnter(out var guardian))
{
    using (guardian)
    {
        // Immediate acquisition succeeded
        ExecuteOperation();
    }
}
else
{
    // Acquisition unattainable
    MitigateUnavailability();
}
```

## Temporal Probe

```csharp
var mutex = new AsyncLock();
var interval = TimeSpan.FromSeconds(5);

var guardian = await mutex.TryEnterAsync(interval);
if (guardian != null)
{
    await using (guardian)
    {
        // Acquisition within interval succeeded
        await ExecuteOperation();
    }
}
else
{
    // Interval elapsed without acquisition
    MitigateExpiration();
}
```

# Interface Specification

## AsyncLock Entity

### Operations

**`Releaser Enter(CancellationToken cancellationToken = default)`**

Synchronously secures the mutex, suspending until attainable; propagates cancellation.

**`bool TryEnter(out Releaser? releaser)`**

Probes for immediate mutex availability sans suspension.

**`ValueTask<Releaser> EnterAsync(CancellationToken cancellationToken = default)`**

Asynchronously secures the mutex, awaiting availability; propagates cancellation.

**`ValueTask<Releaser?> TryEnterAsync(TimeSpan timeout, CancellationToken cancellationToken = default)`**

Asynchronously probes mutex within stipulated interval; returns null on expiration or cancellation.

## Releaser Entity

Implements IDisposable and IAsyncDisposable for deterministic relinquishment. Employs atomic operations to preclude redundant releases. Align disposal modality with acquisition paradigm: synchronous via `using`, asynchronous via `await using`.

## Concurrency Assurance

This construct guarantees thread-safety, enforcing singular possession at any juncture, thereby mitigating race conditions in multi-threaded contexts.

## Efficacy Metrics

Leveraging SemaphoreSlim, it exhibits negligible overhead in low-contention scenarios, efficient queuing under load, and aversion to thread-pool depletion. Optimized for hybrid synchronous-asynchronous deployments.

## Supported Runtimes

- .NET Standard 2.1