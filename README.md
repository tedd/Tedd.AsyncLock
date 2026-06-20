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

## Utilization Modalities

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

var guardian = await mutex.TryEnterAsync(temporalBound);
if (guardian != null)
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

### Releaser Entity Architecture

The `Releaser` object rigorously implements both `IDisposable` and `IAsyncDisposable`. It executes interlocked atomic operations to preclude redundant invocations of the release mechanics. Proper disposal orchestrates the synchronous or asynchronous relinquishment of the `SemaphoreSlim` structure and subsequent repatriation to the `Tedd.ObjectPool`.

## Target Runtimes

- .NET Framework 4.6.2
- .NET Standard 2.0 / 2.1
- .NET 8.0
- .NET 10.0

## Licensing

This framework is distributed under the stipulations of the MIT License.
