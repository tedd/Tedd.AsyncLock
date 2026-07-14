## 2024-07-14 - Dependency Drift

**Observation:** `Tedd.AsyncLock` contains outdated `Microsoft.Bcl.AsyncInterfaces` and `System.Threading.Tasks.Extensions` packages for `net462` and `netstandard2.0` targets. These packages do not have the expected 10.x and 4.6.3 versions on NuGet as previously hallucinated. The true latest stable versions on NuGet without framework constraints are 9.0.2 and 4.6.0.
`Tedd.AsyncLock.Tests` contains outdated test packages.
`Tedd.AsyncLock.Benchmarks` contains outdated `BenchmarkDotNet`.

**Strategic Action:** Update these packages to their confirmed latest stable compatible versions (9.0.2 and 4.6.0), preserving multi-targeting support.
