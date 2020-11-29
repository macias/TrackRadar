using Geo;
using System.Collections.Generic;
using System.Linq;
using static TrackRadar.GpxLoader;

namespace TrackRadar.Implementation
{
#if DEBUG
    public static class GpxToolbox
    {        
        public static void SaveGpxSegments(string filename, IEnumerable<ISegment> segments)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (ISegment seg in segments)
                {
                    writer.WriteTrack($"{idx}:{seg.SectionId}", seg.Points().ToArray());
                    ++idx;
                }
            }
        }
        public static void SaveGpx(string filename, IPlanData plan)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                {
                    int idx = 0;
                    foreach (ISegment seg in plan.Segments)
                    {
                        writer.WriteTrack($"Line {idx}:{seg.SectionId} #{seg.__DEBUG_id}", seg.Points().ToArray());
                        ++idx;
                    }
                }
                {
                    foreach (var cx_entry in plan.Crossroads)
                    {
                        writer.WritePoint($"Point {cx_entry.Value}", cx_entry.Key);
                    }
                }
            }
        }
        
        public static void SaveGpx(string filename, IEnumerable<IEnumerable<GeoPoint>> segments,
            IEnumerable<GeoPoint> waypoints)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                {
                    int idx = 0;
                    foreach (IEnumerable<GeoPoint> seg in segments)
                    {
                        writer.WriteTrack($"Line {idx}", seg.ToArray());
                        ++idx;
                    }
                }
                {
                    int idx = 0;
                    foreach (GeoPoint pt in waypoints)
                    {
                        writer.WritePoint($"Point {idx}", pt);
                        ++idx;
                    }
                }
            }
        }
        public static void SaveGpxSegments(string filename, params IEnumerable<GeoPoint>[] segments)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (IEnumerable<GeoPoint> seg in segments)
                {
                    writer.WriteTrack($"Line {idx}", seg.ToArray());
                    ++idx;
                }
            }
        }
        public static void SaveGpxWaypoints(string filename, IEnumerable<GeoPoint> points)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (GeoPoint pt in points)
                {
                    writer.WritePoint($"{idx}", pt);
                    ++idx;
                }
            }
        }
        internal static void SaveGpxCrossroads(string filename, IEnumerable<Crossroad> points)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (Crossroad cx in points)
                {
                    writer.WritePoint($"{idx}-{cx.Kind}", cx.Point);
                    ++idx;
                }
            }
        }

    }
#endif
}