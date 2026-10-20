using System;
using System.Threading;
using System.Threading.Tasks;
using Tedd;

namespace CodeSnippetsValidation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await AsyncPipeline();
            SyncPipeline();
            await ScopedConstructs();
            await CancellationSemantics();
            await TemporalProbe();
            SyncProbe();
        }

        static async Task AsyncPipeline()
        {
            using var mutex = new AsyncLock();

            await using var guardian = await mutex.EnterAsync();
            // Exclusive asynchronous execution context
            await Task.Yield();
        }

        static void SyncPipeline()
        {
            using var mutex = new AsyncLock();

            using var guardian = mutex.Enter();
            // Exclusive synchronous execution context
            Console.WriteLine("Execution active.");
        }

        static async Task ScopedConstructs()
        {
            using var mutex = new AsyncLock();

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
        }

        static async Task CancellationSemantics()
        {
            using var mutex = new AsyncLock();
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
        }

        static async Task TemporalProbe()
        {
            using var mutex = new AsyncLock();
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
        }

        static void SyncProbe()
        {
            using var mutex = new AsyncLock();

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
        }
    }
}
