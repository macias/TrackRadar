using Geo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TrackRadar.Implementation
{
    internal sealed class GpxDirtyWriter : IGpxDirtyWriter
    {

#if DEBUG
        public static IDisposable Create(string path, out IGpxDirtyWriter writer)
        {
            var stream = System.IO.File.CreateText(path);
            var gpx_writer = new GpxDirtyWriter(stream);
            gpx_writer.WriteHeader();

            writer = gpx_writer;

            return Disposable.Create(() =>
            {
                gpx_writer.Close();
                stream.Dispose();
            });
        }
#endif
        private readonly object threadLock = new object();

        private readonly StreamWriter stream;

        public GpxDirtyWriter(StreamWriter stream)
        {
            this.stream = stream;
        }

        public void WriteHeader()
        {
            lock (this.threadLock)
            {
                this.stream.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                this.stream.WriteLine("<gpx");
                this.stream.WriteLine("version=\"1.0\"");
                this.stream.WriteLine("creator=\"TrackRadar https://github.com/macias/TrackRadar\"");
                this.stream.WriteLine("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
                this.stream.WriteLine("xmlns=\"http://www.topografix.com/GPX/1/0\"");
                this.stream.WriteLine("xsi:schemaLocation=\"http://www.topografix.com/GPX/1/0 http://www.topografix.com/GPX/1/0/gpx.xsd\">");
            }
        }

        public void WriteComment(string comment)
        {
            lock (this.threadLock)
            {
                this.stream.WriteLine($"<!--{comment}-->");
            }
        }

        public void Close()
        {
            lock (this.threadLock)
            {
                this.stream.WriteLine("</gpx>");
            }
        }

        public void WriteLocation(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string name = null, string comment = null)
        {
            lock (this.threadLock)
            {
                stream.Write($"<wpt lat=\"{latitudeDegrees.ToString(CultureInfo.InvariantCulture)}\" lon=\"{longitudeDegrees.ToString(CultureInfo.InvariantCulture)}\"");
                stream.WriteLine($">");
                if (accuracyMeters.HasValue)
                    stream.WriteLine($"<Proximity>{accuracyMeters.Value.ToString(CultureInfo.InvariantCulture)}</Proximity>");
                if (altitudeMeters.HasValue)
                    stream.WriteLine($"<ele>{altitudeMeters.Value.ToString(CultureInfo.InvariantCulture)}</ele>");
                if (name != null)
                    stream.WriteLine($"<name>{name}</name>");
                if (comment != null)
                    stream.WriteLine($"<cmt>{comment}</cmt>");
                stream.WriteLine($"</wpt>");

                stream.Flush();
            }
        }

#if DEBUG
        public void WritePoint(in GeoPoint point, string name, string comment = null)
        {
            this.WriteLocation(point.Latitude.Degrees, point.Longitude.Degrees, altitudeMeters: null, accuracyMeters: null, comment: comment, name: name);
        }

        public void WriteTrack(IEnumerable<GeoPoint> points, string name)
        {
            lock (this.threadLock)
            {
                stream.WriteLine("<trk>");
                stream.WriteLine($"<name>{name}</name>");
                stream.WriteLine("<trkseg>");
                foreach (var point in points)
                {
                    stream.WriteLine($"<trkpt lat=\"{point.Latitude.Degrees.ToString(CultureInfo.InvariantCulture)}\" lon=\"{point.Longitude.Degrees.ToString(CultureInfo.InvariantCulture)}\"/>");
                }
                stream.WriteLine("</trkseg>");
                stream.WriteLine("</trk>");

                stream.Flush();
            }
        }

#endif
    }
}