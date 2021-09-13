using System;
using System.Collections.Generic;
using Geo;
using MathUnit;

namespace TrackRadar.Implementation
{
    public interface IGpxDirtyWriter
    {
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