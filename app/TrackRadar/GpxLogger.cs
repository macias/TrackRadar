using System;
using System.IO;
using Android.Content;
using TrackRadar.Implementation;

namespace TrackRadar
{
    internal sealed class GpxLogger : IDisposable
    {
        private readonly StreamWriter stream_writer;
        private readonly GpxDirtyWriter gpx_writer;

        public GpxLogger(ContextWrapper ctx, string filename, DateTime expires)
        {
            this.stream_writer = HotWriter.CreateStreamWriter(ctx, filename, expires, out bool appended);
            this.gpx_writer = new GpxDirtyWriter(stream_writer);
            if (!appended)
            {
                this.gpx_writer.WriteHeader();
                this.gpx_writer.WriteComment(" CLOSE gpx TAG MANUALLY ");
            }
        }

        public void Dispose()
        {
            this.stream_writer.Dispose();
        }

        public void WriteLocation(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string name = null, string comment = null)
        {
            this.gpx_writer.WriteLocation(latitudeDegrees, longitudeDegrees, altitudeMeters, accuracyMeters, comment:comment, name:name);
            this.stream_writer.Flush();
        }
    }
}