using System;
using System.Threading;
using System.Threading.Tasks;

namespace TrackRadar
{
    public sealed class JobQueue : IDisposable
    {
        private readonly object threadLock = new object();

        private CancelableJob running;

        public JobQueue()
        {

        }

        public void Dispose()
        {
            lock (threadLock)
            {
                this.running?.CancelAsync().Wait();
                this.running?.Dispose();
            }
        }

        internal void Enqueue(Action<CancellationToken> action)
        {
            lock (this.threadLock)
            {
                Func<CancellationToken, Task> asyncAction;
                if (this.running != null && this.running.IsCompleted) // avoiding infinite chain of finished tasks
                {
                    this.running.Dispose();
                    this.running = null;
                }

                if (this.running != null)
                {
                    CancelableJob job = this.running; // anti closure-capture
                    asyncAction = async (CancellationToken token) =>
                    {
                        await job.CancelAsync().ConfigureAwait(false);
                        job.Dispose();
                        action(token);
                    };
                }
                else
                {
                    asyncAction = (CancellationToken token) =>
                    {
                        action(token);
                        return Task.CompletedTask;
                    };
                }

                this.running = new CancelableJob(asyncAction);
            }
        }
    }
}