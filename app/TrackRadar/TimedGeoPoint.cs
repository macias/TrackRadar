using Gpx;

namespace TrackRadar
{
    public struct TimedGeoPoint : IGeoPoint
    {
        public long Ticks { get; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public TimedGeoPoint(long ticks)
        {
            this.Ticks = ticks;
            this.Longitude = 0;
            this.Latitude = 0;
        }

        public override string ToString()
        {
            return Latitude.ToString() + "," + Longitude.ToString();
        }
    }
}