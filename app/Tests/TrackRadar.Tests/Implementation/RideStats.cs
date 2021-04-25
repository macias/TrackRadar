namespace TrackRadar.Tests.Implementation
{
    public readonly struct RideStats
    {
        public double MaxUpdate { get; }
        public double AvgUpdate { get; }
        public int TrackCount { get; }

        public RideStats(double maxUpdate, double avgUpdate,int trackCount)
        {
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