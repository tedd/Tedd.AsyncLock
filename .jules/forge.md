## 2026-09-14 - NuGet Dependency Modernization

**Observation:** Dependencies across `Tedd.AsyncLock`, `Tedd.AsyncLock.Tests`, and `Tedd.AsyncLock.Benchmarks` were outdated. `Tedd.AsyncLock` requires `Microsoft.Bcl.AsyncInterfaces` and `System.Threading.Tasks.Extensions` specifically for `net462` and `netstandard2.0` target frameworks to maintain compatibility.

**Strategic Action:** Updated `Microsoft.Bcl.AsyncInterfaces` to `10.0.12` and `System.Threading.Tasks.Extensions` to `4.6.3` in the main project, respecting the target framework conditions. Updated testing and benchmarking dependencies (`coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `BenchmarkDotNet`) to their latest stable compatible versions.
