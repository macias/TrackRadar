using System.Collections.Generic;
using Geo;

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

        void IGpxDirtyWriter.WriteLocation(double latitudeDegrees, double longitudeDegrees, double? altitudeMeters, double? accuracyMeters, string name, string comment)
        {
        }

#if DEBUG
        void IGpxDirtyWriter.WritePoint(in GeoPoint point, string name, string comment)
        {
        }

        void IGpxDirtyWriter.WriteTrack(IEnumerable<GeoPoint> points, string name)
        {
        }
#endif
    }
}