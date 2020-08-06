using System;
using System.Threading;
using System.Threading.Tasks;

namespace TrackRadar
{
    public sealed class CancelableJob : IDisposable
    {
        private readonly CancellationTokenSource cts;
        private readonly Task task;

        public bool IsCompleted => this.task.IsCompleted;

        public CancelableJob(Func<CancellationToken,Task> asyncAction)
        {
            this.cts = new CancellationTokenSource();
            this.task = Task.Run(() => asyncAction(cts.Token), cts.Token);
        }

        public void Dispose()
        {
            this.cts.Dispose();
        }

        public async Task CancelAsync()
        {
            this.cts.Cancel();
            try
            {
                await this.task.ConfigureAwait(false) ;
            }
            catch
            {
                ; // swallow errors/cancellations
            }
        }
    }
}