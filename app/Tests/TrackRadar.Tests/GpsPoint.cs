using Geo;
using MathUnit;
using System;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    public readonly struct GpsPoint
    {
        public GeoPoint Point { get; }
        public Length? Accuracy { get; }
        public DateTimeOffset? Time { get; }
        public Length? Altitude { get; }

        public GpsPoint(GeoPoint pt, Length? altitude, Length? accuracy, DateTimeOffset? time)
        {
            this.Point = pt;
            Altitude = altitude;
            this.Accuracy = accuracy;
            Time = time;
        }

        internal GpsPoint(ProximityTrackPoint pt) : this(new GeoPoint(pt.Latitude, pt.Longitude),
            pt.Elevation == null ? (Length?)null : Length.FromMeters(pt.Elevation.Value),
            pt.Proximity == null ? (Length?)null : Length.FromMeters(pt.Proximity.Value),
            time: pt.Time)
        {

        }
    }
}
