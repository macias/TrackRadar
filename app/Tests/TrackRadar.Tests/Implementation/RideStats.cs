using MathUnit;
using System.Collections.Generic;

namespace TrackRadar.Tests.Implementation
{
    public readonly struct RideStats
    {
        public IReadOnlyList<Speed> Speeds { get; }
        public double MaxUpdate { get; }
        public double AvgUpdate { get; }
        public int TrackCount { get; }

        public RideStats( IReadOnlyList<Speed> speeds, double maxUpdate, double avgUpdate,int trackCount)
        {
            Speeds = speeds;
            this.MaxUpdate = maxUpdate;
            this.AvgUpdate = avgUpdate;
            TrackCount = trackCount;
        }

        /*public void Deconstruct(out double maxUpdate, out double avgUpdate)
        {
            maxUpdate = this.MaxUpdate;
            avgUpdate = this.AvgUpdate;
        }
        */
    }
}