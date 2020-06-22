using System;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ClockStamper : ITimeStamper
    {
        //public DateTimeOffset StartTime { get; }
        private DateTimeOffset time;

        public long Frequency { get; }

        public ClockStamper(DateTimeOffset start)
        {
            //this.StartTime = start;
            this.time = start;
            this.Frequency = TimeSpan.FromSeconds(1).Ticks;
        }

        public void SetTime(DateTimeOffset dt)
        {
            this.time = dt;
        }

        public void Advance(TimeSpan span)
        {
            this.time += span;
        }

        public long GetTimestamp()
        {
            return this.time.Ticks;
        }
    }
}
