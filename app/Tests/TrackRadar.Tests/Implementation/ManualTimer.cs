using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ManualTimer : ITimer
    {
        private readonly Action callback;

        public ManualTimer(Action callback)
        {
            this.callback = callback;
        }

        public void Dispose()
        {
        }

        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            if (period != System.Threading.Timeout.InfiniteTimeSpan)
                throw new NotImplementedException();
        }

        public void Trigger()
        {
            callback();
        }
    }
}