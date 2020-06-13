using System;
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


        public void WriteLocation(double latitudeDegrees,double longitudeDegrees,string name = null)
        {
            writer.Write($"<wpt lat=\"{latitudeDegrees}\" lon=\"{longitudeDegrees}\"");
            if (name != null)
                writer.Write($" name=\"{name}\"");
            writer.WriteLine($"/>");
        }
    }

}