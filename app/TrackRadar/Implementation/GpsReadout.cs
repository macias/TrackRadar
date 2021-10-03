using Geo;
using MathUnit;

namespace TrackRadar.Implementation
{
    internal readonly struct GpsReadout
    {
        public GeoPoint Point { get; }
        public Length? Altitude { get; }
        public Length? Accuracy { get; }
        public long Timestamp { get; }

        public GpsReadout(GeoPoint point, Length? altitude, Length? accuracy, long timestamp)
        {
            this.Point = point;
            this.Altitude = altitude;
            Accuracy = accuracy;
            this.Timestamp = timestamp;
        }
        public void Deconstruct(out GeoPoint point, out Length? altitude, out Length? accuracy, out long timestamp)
        {
            point = this.Point;
            altitude = this.Altitude;
            accuracy = this.Accuracy;
            timestamp = this.Timestamp;
        }
    }
 
}