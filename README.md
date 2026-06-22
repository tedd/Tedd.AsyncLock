# Tedd.AsyncLock

[![NuGet](https://img.shields.io/nuget/v/Tedd.AsyncLock.svg)](https://www.nuget.org/packages/Tedd.AsyncLock/)

A lightweight, asynchronous-compatible mutex for .NET, facilitating mutual exclusion across synchronous and asynchronous paradigms while incorporating cancellation semantics.

## Installation

Procure via NuGet Package Manager Console:

```
Install-Package Tedd.AsyncLock
```

Via .NET CLI:

```
dotnet add package Tedd.AsyncLock
```

## Salient Features

- **Dual-Mode Compatibility**: Seamlessly interoperates with synchronous and asynchronous workflows.
- **Cancellation Integration**: All acquisition operations respect CancellationToken for abortive control.
- **Non-Blocking Attempts**: Immediate acquisition probes via try-pattern methods.
- **Temporal Bounds**: Asynchronous acquisition with configurable timeout intervals.
- **Concurrent Safety**: Engineered for multi-threaded environments with inherent thread-safety.
- **Resource Management**: Leverages disposable idioms for deterministic lock relinquishment.
- **Optimized Throughput**: Underpinned by SemaphoreSlim for minimal latency in uncontended scenarios.

## Architectural Execution Flow

The framework orchestrates an optimized mutual exclusion paradigm by structurally isolating the execution flow into established mechanical capabilities and planned architectural enhancements.

### Established Mechanics
1. **Semaphore Allocation**: Upon initialization, a singular capacity `SemaphoreSlim` is allocated, inherently supporting both synchronous (`Wait`) and asynchronous (`WaitAsync`) suspension patterns.
2. **Deterministic Renting**: Upon successful lock acquisition, a structural `Releaser` object is vended. This token manages disposal semantics, ensuring singular deterministic release of the underlying lock.
3. **Optimized Pooling**: The `Releaser` tokens are dynamically recycled utilizing an `ObjectPool`, effectively eliminating allocation overhead per structural acquisition sequence, scaling proportional to concurrent hardware logic paths.
4. **Cancellation Flow**: All deterministic await operations integrate intrinsically with the underlying task scheduling system, yielding execution control when cancelled via structural `CancellationToken` hierarchies.

### Architectural Hypotheses (Future Roadmap)
- Implementation of structural thread-affiliation validation to prohibit cross-thread disposal in strictly synchronous scenarios, mitigating speculative programmer error.
- Integration of hierarchical reentrancy structures for advanced multi-stage processing pipelines without localized deadlock anomalies.
- Integration with ambient diagnostic contexts (e.g. `Activity` and `OpenTelemetry`) to trace lock acquisition latency structurally across complex operational fabrics.

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

- .NET Framework 4.6.2
- .NET Standard 2.0
- .NET Standard 2.1
- .NET 8.0
- .NET 10.0

## Licensing Terms

Governed by the MIT License.

## Collaborative Engagement

Augmentations are solicited. Submit anomalies or enhancements via GitHub.