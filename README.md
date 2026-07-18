# Tedd.AsyncLock

[![NuGet](https://img.shields.io/nuget/v/Tedd.AsyncLock.svg)](https://www.nuget.org/packages/Tedd.AsyncLock/)

A highly optimized, asynchronous-compatible mutex designed for .NET applications, facilitating deterministic mutual exclusion across concurrent synchronous and asynchronous execution pipelines while natively integrating temporal bounding and cancellation semantics.

## Architectural Paradigm

The framework operates on a dual-layered architectural foundation designed to maximize throughput and minimize latency overhead.

### Established Mechanics (Functional State)
- **SemaphoreSlim Infrastructure**: The core concurrency control mechanism is anchored by `System.Threading.SemaphoreSlim`, ensuring robust mutual exclusion with nominal overhead during uncontended acquisition paths.
- **Resource Pooling Architecture**: To mitigate allocation pressure, the framework integrates `Tedd.ObjectPool` for the systematic recycling of `Releaser` instances. This deterministic allocation strategy yields zero steady-state garbage collection overhead during operational continuity.

### Planned Enhancements (Architectural Hypotheses)
- **Hierarchical Data Binding**: It is hypothesized that future framework iterations will support hierarchical data binding, enabling distributed locking contexts to automatically inherit constraints from parent application scopes.
- **Routed Event Infrastructure**: A planned integration involves a routed event infrastructure to systematically propagate lock acquisition and relinquishment notifications across distributed microservice topologies.

## Installation Protocol

Procure the dependency via the NuGet Package Manager Console:

```powershell
Install-Package Tedd.AsyncLock
```

Alternatively, integrate via the .NET CLI interface:

```bash
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

### Asynchronous Acquisition Pipeline

```csharp
using System;
using System.Threading.Tasks;
using Tedd;

var mutex = new AsyncLock();

await using var guardian = await mutex.EnterAsync();
// Exclusive asynchronous execution context
await Task.Yield();
```

### Synchronous Acquisition Pipeline

```csharp
using System;
using Tedd;

var mutex = new AsyncLock();

using var guardian = mutex.Enter();
// Exclusive synchronous execution context
Console.WriteLine("Execution active.");
```

### Scoped Acquisition Constructs

```csharp
using System;
using System.Threading.Tasks;
using Tedd;

var mutex = new AsyncLock();

// Asynchronous execution scope
await using (var guardian = await mutex.EnterAsync())
{
    await Task.Yield();
}

// Synchronous execution scope
using (var guardian = mutex.Enter())
{
    Console.WriteLine("Execution active.");
}
```

### Cancellation and Temporal Constraints

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Tedd;

var mutex = new AsyncLock();
using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await using var guardian = await mutex.EnterAsync(cancellationSource.Token);
    await Task.Delay(100, CancellationToken.None);
}
catch (OperationCanceledException)
{
    // Execute mitigation protocols upon abortive interruption
}
```

### Deterministic Temporal Probe

```csharp
using System;
using System.Threading.Tasks;
using Tedd;

var mutex = new AsyncLock();
var temporalBound = TimeSpan.FromSeconds(2);

if (await mutex.TryEnterAsync(temporalBound) is { } guardian)
{
    await using (guardian)
    {
        // Mutex successfully secured within the stipulated interval
        await Task.Yield();
    }
}
else
{
    // Execute mitigation protocols due to temporal expiration
}
```

### Non-Blocking Synchronous Probe

```csharp
using System;
using Tedd;

var mutex = new AsyncLock();

if (mutex.TryEnter(out var guardian))
{
    using (guardian)
    {
        // Immediate acquisition achieved
        Console.WriteLine("Execution active.");
    }
}
else
{
    // Execute mitigation protocols upon acquisition failure
}
```

## Interface Specifications

### AsyncLock Entity Operations

- **`Releaser Enter(CancellationToken cancellationToken = default)`**: Synchronously secures the mutex; strictly propagates cancellation exceptions.
- **`bool TryEnter(out Releaser? releaser)`**: Probes for instantaneous mutex availability devoid of thread suspension.
- **`ValueTask<Releaser> EnterAsync(CancellationToken cancellationToken = default)`**: Asynchronously secures the mutex; strictly propagates cancellation exceptions.
- **`ValueTask<Releaser?> TryEnterAsync(TimeSpan timeout, CancellationToken cancellationToken = default)`**: Asynchronously evaluates mutex availability bounded by a stipulated temporal interval; yields null upon expiration.
- **`void Dispose()`**: Synchronously disposes of the underlying semaphore.
- **`ValueTask DisposeAsync()`**: Asynchronously disposes of the underlying semaphore if supported, otherwise executes synchronous disposal.

### Releaser Entity Architecture

The `Releaser` object rigorously implements both `IDisposable` and `IAsyncDisposable`. It executes interlocked atomic operations to preclude redundant invocations of the release mechanics. Proper disposal orchestrates the synchronous or asynchronous relinquishment of the `SemaphoreSlim` structure and subsequent repatriation to the `Tedd.ObjectPool`.

## Target Runtimes

- .NET Framework 4.6.2
- .NET Standard 2.0 / 2.1
- .NET 8.0
- .NET 10.0

## Licensing

This framework is distributed under the stipulations of the MIT License.
