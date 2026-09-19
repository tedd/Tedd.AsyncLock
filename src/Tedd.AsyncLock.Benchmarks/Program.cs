using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tedd.Benchmarks
{
    [MemoryDiagnoser]
    public class AsyncLockBenchmark
    {
        private Tedd.Archive.AsyncLockLegacy _archiveLock = new();
        private Tedd.AsyncLock _currentLock = new();

        [Benchmark(Baseline = true)]
        public async Task Archive_DisposeAsyncReleaser()
        {
            var r = await _archiveLock.EnterAsync();
            await r.DisposeAsync();
        }

        [Benchmark]
        public async Task Current_DisposeAsyncReleaser()
        {
            var r = await _currentLock.EnterAsync();
            await r.DisposeAsync();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<AsyncLockBenchmark>();
        }
    }
}
