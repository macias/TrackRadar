using Geo;

namespace TrackRadar.Implementation
{
    public sealed partial class GpxLoader
    {
        private readonly struct TurnNodeInfo
        {
            public GeoPoint TurnPoint { get; }
            // 0 for immediate connection between turn and node, 1 when node has to go through immediate node, etc.
            public int Hops { get; }

            public TurnNodeInfo(GeoPoint turnPoint, int hops)
            {
                TurnPoint = turnPoint;
                Hops = hops;
            }

            public void Deconstruct( out GeoPoint turnPoint, out int hops)
            {
                turnPoint = this.TurnPoint;
                hops = this.Hops;
            }
        }
    }
}