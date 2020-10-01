using System.Collections.Generic;
using Geo;
using Gpx;
using MathUnit;

namespace TrackRadar
{
    internal static class PositionCalculator
    {
        /// <param name="dist">negative value means on track</param>
     /*   public static bool IsOnTrack<T>(T point, IReadOnlyList<ITrackSegment> trackSegments,
            int offTrackDistance, // meters
            out double dist) // meters
            where T : XGeoPoint
        {
            dist = double.MaxValue;
            int closest_track = 0;
            int closest_segment = 0;

            //float accuracy_offset = Math.Max(0, location.Accuracy-statistics.Accuracy);

            for (int t = 0; t < trackSegments.Count; ++t)
            {
                ITrackSegment seg = trackSegments[t];
                for (int s = seg.TrackPoints.Count - 1; s > 0; --s)
                {
                    double d = point.GetDistanceToArcSegment(seg.TrackPoints[s - 1], seg.TrackPoints[s]).Meters;

                    if (dist > d)
                    {
                        dist = d;
                        closest_track = t;
                        closest_segment = s;
                    }

                    if (d <= offTrackDistance)
                    {
                        //logDebug(LogLevel.Verbose, $"On [{s}]" + d.ToString("0.0") + " (" + seg.TrackPoints[s - 1].ToString(geoPointFormat) + " -- "
                        //  + seg.TrackPoints[s].ToString(geoPointFormat) + ") in " + watch.Elapsed.ToString());
                        dist = -dist;
                        return true;
                    }
                }
            }


            //this.serviceLog.WriteLine(LogLevel.Verbose, $"dist {dist.ToString("0.0")} point {point.ToString(geoPointFormat)}"
            //  + $" segment {trackSegments[closest_track].TrackPoints[closest_segment - 1].ToString(geoPointFormat)}"
            // + $" -- {trackSegments[closest_track].TrackPoints[closest_segment].ToString(geoPointFormat)}");
            //logDebug(LogLevel.Verbose, $"Off [{closest_segment}]" + dist.ToString("0.0") + " in " + watch.Elapsed.ToString());
            return false;
        }
        */
        internal static bool IsOnTrack(in GeoPoint point, IGeoMap map, Length offTrackDistance, 
            out ISegment segment, out double fenceDistance,out GeoPoint crosspoint)
        {
            crosspoint = default;
            segment = default;
            //bool res = map.IsWithinLimit(point, offTrackDistance, out Length? map_dist);
            bool res = map.FindClosest(point, offTrackDistance, out segment, out Length? map_dist, out crosspoint);
            if (res)
                fenceDistance = -map_dist.Value.Meters;
            else if (map_dist.HasValue)
                fenceDistance = map_dist.Value.Meters;
            else
                fenceDistance = double.PositiveInfinity;

            return res;
        }
    }
}