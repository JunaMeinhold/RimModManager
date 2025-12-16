namespace RimModManager
{
    using System.Threading.Tasks;

    public static class SemaphoreSlimExtensions
    {
        public struct LockGuard : IDisposable, IAsyncDisposable
        {
            private SemaphoreSlim? semaphore;

            public LockGuard(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Reset()
            {
                semaphore?.Release();
                semaphore = null;
            }

            public void Dispose()
            {
                Reset();
            }

            public ValueTask DisposeAsync()
            {
                Reset();
                return ValueTask.CompletedTask;
            }
        }

        public static async ValueTask<LockGuard> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new LockGuard(semaphore);
        }

        public static LockGuard Lock(this SemaphoreSlim semaphore)
        {
            semaphore.Wait();
            return new LockGuard(semaphore);
        }
    }
}
