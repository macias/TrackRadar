using System;
using System.Collections.Generic;
using Geo;
using MathUnit;

namespace TrackRadar.Implementation
{
    public sealed class NoneGpxDirtyWriter : IGpxDirtyWriter
    {
        public static IGpxDirtyWriter Instance { get; } = new NoneGpxDirtyWriter();

        private NoneGpxDirtyWriter()
        {

        }

        void IGpxDirtyWriter.WriteComment(string comment)
        {
        }

        void IGpxDirtyWriter.WriteWaypoint(double latitudeDegrees, double longitudeDegrees, double? altitudeMeters,
            double? accuracyMeters, string name, string comment, DateTimeOffset? time)
        {
        }

        void IGpxDirtyWriter.WriteTrackPoint(double latitudeDegrees, double longitudeDegrees, double? altitudeMeters,
            double? accuracyMeters, string name, string comment, DateTimeOffset? time)
        {
        }

#if DEBUG
        void IGpxDirtyWriter.WriteWaypoint(in GeoPoint point, string name, string comment, Length? accuracy)
        {
        }

        void IGpxDirtyWriter.WriteTrack(IEnumerable<GeoPoint> points, string name)
        {
        }
#endif
    }
}