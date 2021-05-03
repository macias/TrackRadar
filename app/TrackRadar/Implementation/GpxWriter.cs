using System;
using System.Globalization;
using Android.Content;
using Android.Locations;

namespace TrackRadar.Implementation
{
    internal sealed class GpxWriter : IDisposable
    {
        private readonly HotWriter writer;

        public GpxWriter(ContextWrapper ctx, string filename, DateTime expires)
        {
            this.writer = new HotWriter(ctx, filename, expires, out bool appended);
            if (!appended)
            {
                this.writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                this.writer.WriteLine("<gpx");
                this.writer.WriteLine("version=\"1.0\"");
                this.writer.WriteLine("creator=\"TrackRadar https://github.com/macias/TrackRadar\"");
                this.writer.WriteLine("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
                this.writer.WriteLine("xmlns=\"http://www.topografix.com/GPX/1/0\"");
                this.writer.WriteLine("xsi:schemaLocation=\"http://www.topografix.com/GPX/1/0 http://www.topografix.com/GPX/1/0/gpx.xsd\">");
                this.writer.WriteLine("<!-- CLOSE gpx TAG MANUALLY -->");
            }
        }

        public void Dispose()
        {
            this.writer.Dispose();
        }


        public void WriteLocation(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string comment = null, string name = null)
        {
            writer.Write($"<wpt lat=\"{latitudeDegrees.ToString(CultureInfo.InvariantCulture)}\" lon=\"{longitudeDegrees.ToString(CultureInfo.InvariantCulture)}\"");
            writer.WriteLine($">");
            if (accuracyMeters.HasValue)
                writer.WriteLine($"<Proximity>{accuracyMeters.Value.ToString(CultureInfo.InvariantCulture)}</Proximity>");
            if (altitudeMeters.HasValue)
                writer.WriteLine($"<ele>{altitudeMeters.Value.ToString(CultureInfo.InvariantCulture)}</ele>");
            if (name != null)
                writer.WriteLine($"<name>{name}</name>");
            if (comment != null)
                writer.WriteLine($"<cmt>{comment}</cmt>");
            writer.WriteLine($"</wpt>");
        }
    }

}