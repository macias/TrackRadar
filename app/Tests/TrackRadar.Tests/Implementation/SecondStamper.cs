using System;

namespace TrackRadar.Tests.Implementation
{
    // here we have stamper which goes in 1 seconds interval, so single increase of the time equal 1 second
    // the choice of such relation is for easier GPS ticking
    internal sealed class SecondStamper : ITimeStamper
    {
        private long time;

        public long Frequency { get; }

        public SecondStamper()
        {
            this.Frequency = 1;
        }

        public void Advance()
        {
            ++this.time;
        }

        public long GetTimestamp()
        {
            return this.time;
        }
    }
}
