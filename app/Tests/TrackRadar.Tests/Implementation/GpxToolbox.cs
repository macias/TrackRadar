using Geo;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;
using static TrackRadar.Implementation.GpxLoader;

namespace TrackRadar.Tests.Implementation
{
#if DEBUG
    public static class GpxToolbox
    {
        public static void SaveGpxSegments(string filename, IEnumerable<ISegment> segments)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                int idx = 0;
                foreach (ISegment seg in segments)
                {
                    writer.WriteTrack(seg.Points().ToArray(), $"{idx}:{seg.SectionId}");
                    ++idx;
                }
            }
        }
        public static void SaveGpx(string filename, IPlanData plan)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                {
                    int idx = 0;
                    foreach (ISegment seg in plan.Segments)
                    {
                        writer.WriteTrack(seg.Points().ToArray(), $"Line {idx}:{seg.SectionId} #{seg.__DEBUG_id}");
                        ++idx;
                    }
                }
                {
                    foreach (var cx_entry in plan.Crossroads)
                    {
                        writer.WriteWaypoint(cx_entry.Key, $"Point {cx_entry.Value}");
                    }
                }
            }
        }

        public static void SaveGpx(string filename, IEnumerable<IEnumerable<GeoPoint>> segments,
            IEnumerable<GeoPoint> waypoints)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                {
                    int idx = 0;
                    foreach (IEnumerable<GeoPoint> seg in segments)
                    {
                        writer.WriteTrack(seg.ToArray(), $"Line {idx}");
                        ++idx;
                    }
                }
                {
                    int idx = 0;
                    foreach (GeoPoint pt in waypoints)
                    {
                        writer.WriteWaypoint(pt, $"Point {idx}");
                        ++idx;
                    }
                }
            }
        }
        public static void SaveGpxSegments(string filename, params IEnumerable<GeoPoint>[] segments)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                int idx = 0;
                foreach (IEnumerable<GeoPoint> seg in segments)
                {
                    writer.WriteTrack(seg.ToArray(), $"Line {idx}");
                    ++idx;
                }
            }
        }
        public static void SaveGpxWaypoints(string filename, IEnumerable<GpsPoint> points)
        {
            SaveGpxWaypoints(filename, points.Zip(Enumerable.Range(0, int.MaxValue), (p, i) => (p, i.ToString())));
        }

        public static void SaveGpxWaypoints(string filename, IEnumerable<(GpsPoint pt, string name)> points)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                int idx = 0;
                foreach ((GpsPoint pt, string name) in points)
                {
                    writer.WriteWaypoint(pt.Point, name:name, accuracy: pt.Accuracy);
                    ++idx;
                }
            }
        }
        internal static void SaveGpxCrossroads(string filename, IEnumerable<Crossroad> points)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                int idx = 0;
                foreach (Crossroad cx in points)
                {
                    writer.WriteWaypoint(cx.Point, $"{idx}-{cx.Kind}");
                    ++idx;
                }
            }
        }

    }
#endif
}