using System;
using System.Threading;
using System.Threading.Tasks;
using Tedd;

class Program
{
    static async Task Main()
    {
        // Deterministic Temporal Probe
        {
            var mutex = new AsyncLock();
            var temporalBound = TimeSpan.FromSeconds(2);

            if (await mutex.TryEnterAsync(temporalBound) is { } guardian)
            {
                await using var _ = guardian;
                // Mutex successfully secured within the stipulated interval
                await Task.Yield();
            }
            else
            {
                // Execute mitigation protocols due to temporal expiration
            }
        }

        // Non-Blocking Synchronous Probe
        {
            var mutex = new AsyncLock();

            if (mutex.TryEnter(out var guardian))
            {
                using var _ = guardian;
                // Immediate acquisition achieved
                Console.WriteLine("Execution active.");
            }
            else
            {
                // Execute mitigation protocols upon acquisition failure
            }
        }
    }
}
