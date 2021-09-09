using System;
using System.Collections.Generic;
using Geo;

namespace TrackRadar.Implementation
{
    public interface IGpxDirtyWriter
    {
        void WriteComment(string comment);
        void WriteLocation(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string name = null, string comment = null, DateTimeOffset? time = null);
#if DEBUG
        void WritePoint(in GeoPoint point, string name, string comment = null);
        void WriteTrack(IEnumerable<GeoPoint> points, string name);
#endif
    }
}