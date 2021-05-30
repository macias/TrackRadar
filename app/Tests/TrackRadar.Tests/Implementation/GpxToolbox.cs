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
            using (GpxDirtyWriter.Create(filename,out IGpxDirtyWriter writer))
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
                        writer.WritePoint(cx_entry.Key, $"Point {cx_entry.Value}");
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
                        writer.WritePoint(pt, $"Point {idx}");
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
        public static void SaveGpxWaypoints(string filename, IEnumerable<GeoPoint> points)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                int idx = 0;
                foreach (GeoPoint pt in points)
                {
                    writer.WritePoint(pt, $"{idx}");
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
                    writer.WritePoint(cx.Point, $"{idx}-{cx.Kind}");
                    ++idx;
                }
            }
        }

    }
#endif
}