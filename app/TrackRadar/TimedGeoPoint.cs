using Gpx;

namespace TrackRadar
{
    public sealed class TimedGeoPoint : GeoPoint
    {
        public long Ticks { get; }

        public TimedGeoPoint(long ticks)
        {
            this.Ticks = ticks;
        }
    }
}