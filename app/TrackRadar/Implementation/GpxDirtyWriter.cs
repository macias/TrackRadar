using Geo;
using Gpx;
using MathUnit;
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

        public void WriteRaw(string value)
        {
            lock (this.threadLock)
            {
                this.stream.Write(value);
            }
        }

        public void Close()
        {
            lock (this.threadLock)
            {
                this.stream.WriteLine("</gpx>");
            }
        }

        public void WriteWaypoint(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string name = null,
            string comment = null, DateTimeOffset? time = null)
        {
            writePoint(GpxSymbol.Waypoint,
                latitudeDegrees: latitudeDegrees,
                longitudeDegrees: longitudeDegrees,
                altitudeMeters: altitudeMeters,
                accuracyMeters: accuracyMeters,
                name: name,
                comment: comment,
                time: time);
        }

        public void WriteTrackPoint(double latitudeDegrees, double longitudeDegrees,
            double? altitudeMeters = null, double? accuracyMeters = null, string name = null,
            string comment = null, DateTimeOffset? time = null)
        {
            writePoint(GpxSymbol.TrackPoint,
                latitudeDegrees: latitudeDegrees,
                longitudeDegrees: longitudeDegrees,
                altitudeMeters: altitudeMeters,
                accuracyMeters: accuracyMeters,
                name: name,
                comment: comment,
                time: time);
        }

        private void writePoint(string tag, double latitudeDegrees, double longitudeDegrees,
    double? altitudeMeters = null, double? accuracyMeters = null, string name = null, string comment = null, DateTimeOffset? time = null)
        {
            lock (this.threadLock)
            {
                stream.Write($"<{tag} {GpxSymbol.Latitude}=\"{latitudeDegrees.ToString(CultureInfo.InvariantCulture)}\" {GpxSymbol.Longitude}=\"{longitudeDegrees.ToString(CultureInfo.InvariantCulture)}\"");
                stream.WriteLine($">");
                if (accuracyMeters.HasValue)
                    stream.WriteLine($"<{GpxSymbol.Proximity}>{accuracyMeters.Value.ToString(CultureInfo.InvariantCulture)}</{GpxSymbol.Proximity}>");
                if (altitudeMeters.HasValue)
                    stream.WriteLine($"<{GpxSymbol.Elevation}>{altitudeMeters.Value.ToString(CultureInfo.InvariantCulture)}</{GpxSymbol.Elevation}>");
                if (name != null)
                    stream.WriteLine($"<{GpxSymbol.Name}>{name}</{GpxSymbol.Name}>");
                if (time != null)
                    stream.WriteLine($"<{GpxSymbol.Time}>{(Formatter.ZuluFormat(time.Value))}</{GpxSymbol.Time}>");
                if (comment != null)
                    stream.WriteLine($"<{GpxSymbol.Comment}>{comment}</{GpxSymbol.Comment}>");
                stream.WriteLine($"</{tag}>");

                stream.Flush();
            }
        }

#if DEBUG
        public void WriteWaypoint(in GeoPoint point, string name, string comment = null,Length? accuracy = null)
        {
            this.WriteWaypoint(point.Latitude.Degrees, point.Longitude.Degrees, altitudeMeters: null, 
                accuracyMeters: accuracy?.Meters, comment: comment, name: name);
        }

        public void WriteTrack(IEnumerable<GeoPoint> points, string name)
        {
            lock (this.threadLock)
            {
                stream.WriteLine($"<{GpxSymbol.Track}>");
                stream.WriteLine($"<{GpxSymbol.Name}>{name}</{GpxSymbol.Name}>");
                stream.WriteLine($"<{GpxSymbol.TrackSegment}>");
                foreach (var point in points)
                {
                    WriteTrackPoint(latitudeDegrees: point.Latitude.Degrees, longitudeDegrees: point.Longitude.Degrees);
                }
                stream.WriteLine($"</{GpxSymbol.TrackSegment}>");
                stream.WriteLine($"</{GpxSymbol.Track}>");

                stream.Flush();
            }
        }

#endif
    }
}