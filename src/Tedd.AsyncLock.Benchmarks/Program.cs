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
        private Tedd.Archive.AsyncLock _archiveLock = new();
        private Tedd.AsyncLock _currentLock = new();

        [Benchmark(Baseline = true)]
        public async Task Archive_EnterAsync()
        {
            await using var releaser = await _archiveLock.EnterAsync();
        }

        [Benchmark]
        public async Task Current_EnterAsync()
        {
            await using var releaser = await _currentLock.EnterAsync();
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
