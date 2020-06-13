using Geo;
using Gpx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TrackRadar.Implementation
{
    internal sealed class GpxDirtyWriter : IDisposable
    {
        private readonly StreamWriter file;

        public GpxDirtyWriter(string path)
        {
            this.file = new System.IO.StreamWriter(path);

            file.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            file.WriteLine("<gpx");
            file.WriteLine("version=\"1.0\"");
            file.WriteLine("creator=\"TrackRadar https://github.com/macias/TrackRadar\"");
            file.WriteLine("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            file.WriteLine("xmlns=\"http://www.topografix.com/GPX/1/0\"");
            file.WriteLine("xsi:schemaLocation=\"http://www.topografix.com/GPX/1/0 http://www.topografix.com/GPX/1/0/gpx.xsd\">");
        }
        public void Dispose()
        {
            file.WriteLine("</gpx>");
            file.Dispose();
        }

        public void WriteTrack(string name, params GeoPoint[] points)
        {
            file.WriteLine("<trk>");
            file.WriteLine($"<name>{name}</name>");
            file.WriteLine("<trkseg>");
            foreach (var point in points)
            {
                file.WriteLine($"<trkpt {str(point)}/>");
            }
            file.WriteLine("</trkseg>");
            file.WriteLine("</trk>");
        }
        public void WritePoint(string name,in GeoPoint point)
        {
            file.WriteLine($"<wpt {str(point)}>");
            if (name != null)
                file.WriteLine($"<name>{name}</name>");
            file.WriteLine("</wpt>");
        }

        private string str(in GeoPoint point)
        {
            return $"lat=\"{point.Latitude.Degrees.ToString(CultureInfo.InvariantCulture)}\" lon=\"{point.Longitude.Degrees.ToString(CultureInfo.InvariantCulture)}\"";
        }

    }
}