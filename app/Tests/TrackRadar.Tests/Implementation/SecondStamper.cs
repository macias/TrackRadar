using System;
using System.Diagnostics;

namespace TrackRadar.Tests.Implementation
{
    public delegate void TimeEventHandler(object sender, TimeSpan time);

    // here we have stamper which goes in 1 seconds interval, so single increase of the time equal 1 second
    // the choice of such relation is for easier GPS ticking
    internal sealed class SecondStamper : TrackRadar.ITimeStamper
    {
        private long time;

        public long Frequency { get; }

        public event TimeEventHandler TimePassed;

        public SecondStamper()
        {
            this.time = Stopwatch.GetTimestamp();   // get realistic starting value
            this.Frequency = 1;
        }

        public void Advance()
        {
            ++this.time;
            TimePassed?.Invoke(this, TimeSpan.FromSeconds(1));
        }

        public long GetTimestamp()
        {
            return this.time;
        }
    }
}
