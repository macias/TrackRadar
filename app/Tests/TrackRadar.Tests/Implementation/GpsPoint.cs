using Geo;
using MathUnit;

namespace TrackRadar.Tests.Implementation
{
    public readonly struct GpsPoint
    {
        public GeoPoint Point { get; }
        public Length? Accuracy { get; }

        public GpsPoint(GeoPoint pt, Length? accuracy)
        {
            this.Point = pt;
            this.Accuracy = accuracy;
        }

        internal GpsPoint(ProximityTrackPoint pt) : this(new GeoPoint(pt.Latitude, pt.Longitude),
            pt.Proximity == null ? (Length?)null : Length.FromMeters(pt.Proximity.Value))
        {

        }
    }
}
