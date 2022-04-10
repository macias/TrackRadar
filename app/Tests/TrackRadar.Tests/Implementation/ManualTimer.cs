using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ManualTimer : ITimer
    {
        private readonly Action callback;
        public TimeSpan? DueTime { get; private set; }

        public ManualTimer(Action callback, SecondStamper stamper)
        {
            this.callback = callback;
            stamper.TimePassed += Stamper_TimePassed;
        }

        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            if (period != System.Threading.Timeout.InfiniteTimeSpan)
                throw new NotSupportedException();

            this.DueTime = dueTime;
        }

        private void Stamper_TimePassed(object sender, TimeSpan time)
        {
            if (DueTime.HasValue)
            {
                DueTime = DueTime.Value - time;
                if (DueTime.Value<=TimeSpan.Zero)
                {
                    DueTime = null;
                    callback();
                }
            }
        }

        public void Dispose()
        {
        }


/*        public void TriggerCallback()
        {
            callback();
        }*/
    }
}