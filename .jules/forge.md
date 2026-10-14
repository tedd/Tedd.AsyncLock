## 2025-01-20 - NuGet Dependency Modernization

**Observation:** The core library `Tedd.AsyncLock.csproj` relies on outdated compatibility packages for `net462` and `netstandard2.0`: `Microsoft.Bcl.AsyncInterfaces` (8.0.0) and `System.Threading.Tasks.Extensions` (4.5.4). Test (`Tedd.AsyncLock.Tests.csproj`) and benchmark (`Tedd.AsyncLock.Benchmarks.csproj`) projects also use outdated packages such as `coverlet.collector` (6.0.0), `Microsoft.NET.Test.Sdk` (17.8.0), `xunit` (2.5.3), `xunit.runner.visualstudio` (2.5.3), and `BenchmarkDotNet` (0.13.12).

**Strategic Action:** Update dependencies to their latest stable versions without altering the target framework surface area (`net462`, `netstandard2.0`, `netstandard2.1`, `net8.0`, `net10.0`). The core compatibility references will remain partitioned using `Condition="'$(TargetFramework)' == 'net462' Or '$(TargetFramework)' == 'netstandard2.0'"`. Updated versions include:
- `Microsoft.Bcl.AsyncInterfaces`: 10.0.13
- `System.Threading.Tasks.Extensions`: 4.6.3
- `coverlet.collector`: 10.1.0
- `Microsoft.NET.Test.Sdk`: 18.11.0
- `xunit`: 2.9.3
- `xunit.runner.visualstudio`: 4.0.1
- `BenchmarkDotNet`: 0.15.8
