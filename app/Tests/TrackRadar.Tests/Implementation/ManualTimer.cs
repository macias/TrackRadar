using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ManualTimer : ITimer
    {
        private readonly Action callback;
        private TimeSpan? dueTime;

        public ManualTimer(Action callback, SecondStamper stamper)
        {
            this.callback = callback;
            stamper.TimePassed += Stamper_TimePassed;
        }

        private void Stamper_TimePassed(object sender, TimeSpan time)
        {
            if (dueTime.HasValue)
            {
                dueTime = dueTime.Value - time;
                if (dueTime.Value<=TimeSpan.Zero)
                {
                    dueTime = null;
                    callback();
                }
            }
        }

        public void Dispose()
        {
        }

        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            if (period != System.Threading.Timeout.InfiniteTimeSpan)
                throw new NotSupportedException();

            this.dueTime = dueTime;
        }

/*        public void TriggerCallback()
        {
            callback();
        }*/
    }
}