using Geo;
using MathUnit;
using System;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    public interface IGpxDirtyWriter
    {
        void WriteRaw(string value);
        void WriteComment(string comment);
        void WriteWaypoint(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string name = null, string comment = null, DateTimeOffset? time = null);
        void WriteTrackPoint(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string name = null, string comment = null, DateTimeOffset? time = null);
#if DEBUG
        void WriteWaypoint(in GeoPoint point, string name, string comment = null, Length? accuracy = null);
        void WriteTrack(IEnumerable<GeoPoint> points, string name);
#endif
    }
}