using Gpx;

namespace TrackRadar
{
    public struct TimedGeoPoint : IGeoPoint
    {
        public long Ticks { get; }
        public Angle Latitude { get; set; }
        public Angle Longitude { get; set; }

        public TimedGeoPoint(long ticks)
        {
            this.Ticks = ticks;
            this.Longitude = Angle.Zero;
            this.Latitude = Angle.Zero;
        }

        public override string ToString()
        {
            return Latitude.ToString() + "," + Longitude.ToString();
        }
    }
}