using System;
using System.Threading;

namespace TrackRadar.Implementation
{    
    internal sealed class WrapTimer : ITimer
    {
        private readonly Timer timer;

        public WrapTimer(Action callback)
        {
            this.timer = new Timer(_ => callback());
        }

        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            timer.Change(dueTime, period);
        }

        public void Dispose()
        {
            using (var handle = new AutoResetEvent(false))
            {
                if (this.timer.Dispose(handle))
                    handle.WaitOne();
            }
        }
    }
}