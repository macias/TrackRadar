using Gpx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrackRadar
{
    internal partial class GpxLoader
    {
        private static readonly Length numericAccuracy = Length.FromMeters(1);

        public static GpxData ReadGpx(string filename, Length offTrackDistance, Action<Exception> onError)
        {
            return ReadGpxAsync(filename, offTrackDistance, onError).Result;
        }
        public static async Task<GpxData> ReadGpxAsync(string filename, Length offTrackDistance, Action<Exception> onError)
        {
            var tracks = new List<GpxTrackSegment>();
            var waypoints = new List<IGeoPoint>();

            using (var input = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                using (IGpxReader reader = GpxReaderFactory.Create(input))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        switch (reader.ObjectType)
                        {
                            case GpxObjectType.Metadata:
                                break;
                            case GpxObjectType.WayPoint:
                                waypoints.Add(reader.WayPoint);
                                break;
                            case GpxObjectType.Route:
                                break;
                            case GpxObjectType.Track:
                                tracks.AddRange(reader.Track.Segments);
                                break;
                        }
                    }

                }

            }

            try
            {
                // add only distant intersections to user already marked waypoints
                IEnumerable<IGeoPoint> crossroads = filterDistant(waypoints,
                    findCrossroads(tracks, offTrackDistance),
                    offTrackDistance);
                waypoints.AddRange(crossroads);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }

            return new GpxData() { Tracks = tracks, Crossroads = waypoints };
        }

        private static IEnumerable<IGeoPoint> filterDistant(IEnumerable<IGeoPoint> fixedPoints,
            IEnumerable<IGeoPoint> incoming, Length limit)
        {
            foreach (IGeoPoint pt in incoming)
                if (fixedPoints.All(it => it.GetDistance(pt) > limit))
                    yield return pt;
        }

        private static IEnumerable<IGeoPoint> findCrossroads(List<GpxTrackSegment> tracks, Length offTrackDistance)
        {
            var crossroads = new List<Crossroad>();
            for (int i = 0; i < tracks.Count; ++i)
                for (int k = i + 1; k < tracks.Count; ++k)
                {
                    // mark each intersection with source index, this will allow us better averaging the points
                    crossroads.AddRange(getIntersections(tracks[i], tracks[k], offTrackDistance)
                        .Select(it => { it.SourceIndex = Tuple.Create(i, k); return it; }));
                }

            while (averageNearby(crossroads, offTrackDistance, onlySameTracks: true))
            {
                ;
            }
            while (averageNearby(crossroads, offTrackDistance, onlySameTracks: false))
            {
                ;
            }

            // do not export extensions of the tracks, only true intersections or passing by "> * <"
            // which we treat as almost-intersection
            return crossroads.Where(it => it.Kind != CrossroadKind.Extension).Select(it => it.Point);
        }

        private static bool averageNearby(List<Crossroad> points, Length limit, bool onlySameTracks)
        {
            bool changed = false;

            for (int i = 0; i < points.Count; ++i)
                for (int k = i + 1; k < points.Count; ++k)
                {
                    Crossroad a = points[i];
                    Crossroad b = points[k];

                    bool same_tracks = Object.Equals(a.SourceIndex, b.SourceIndex) && a.SourceIndex != null;
                    if (onlySameTracks && !same_tracks)
                        continue;

                    if (GeoCalculator.GetDistance(a.Point, b.Point) < limit)
                    {
                        CrossroadKind kind = CrossroadKind.Intersection;
                        if (same_tracks && a.Kind != CrossroadKind.Intersection && b.Kind != CrossroadKind.Intersection)
                        {
                            // having two intersection nearby we check if those are from the same pairs of tracks
                            // if yes, we treat them as extension if just one of them is extension, this way we avoid
                            // spurious results with a lot of intersections when in reality they are cause by imperfections
                            // or curvature of the connecting tracks, like this:
                            //              ------
                            //             /
                            //  ----------*
                            // if the upper part is angled a bit, we would get another intersection
                            // since the already marked one is found and it is extension, that another one is merged
                            // and give in result extension

                            // as for the passing-by this condition simply says, that if there is extension and passing-by
                            // point nearby the outcome is extension, because that passing-by is the effect of the curve
                            // of the track (see picture above)
                            if (a.Kind == CrossroadKind.Extension || b.Kind == CrossroadKind.Extension)
                                kind = CrossroadKind.Extension;
                            // also we have to deal with passing-by points like in shape "> * <"
                            // this is actually "else" case but we add this condition as sanity check
                            else if (a.Kind == CrossroadKind.PassingBy || b.Kind == CrossroadKind.PassingBy)
                                kind = CrossroadKind.PassingBy;
                            else
                                throw new NotImplementedException($"This case is not possible, right? {a.Kind}, {b.Kind}");
                        }

                        points[i] = new Crossroad
                        {
                            Point = GeoCalculator.GetMidPoint(a.Point, b.Point),
                            Kind = kind,
                            SourceIndex = same_tracks ? a.SourceIndex : null
                        };
                        points[k] = points[points.Count - 1];
                        points.RemoveAt(points.Count - 1);

                        changed = true;
                    }
                }

            return changed;
        }

        private static IGeoPoint getIntersection(IGeoPoint track10, IGeoPoint track11,
            IGeoPoint track20, IGeoPoint track21, out Length cx_len1_0)
        {
            IGeoPoint cx1, cx2;
            GeoCalculator.GetArcIntersection(track10, track11, track20, track21, out cx1, out cx2);
            if (cx1 == null)
            {
                cx_len1_0 = Length.Zero;
                return null;
            }

            Length cx1_len = cx1.GetDistance(track10);
            Length cx2_len = cx2.GetDistance(track10);
            if (cx1_len < cx2_len)
            {
                cx_len1_0 = cx1_len;
                return cx1;
            }
            else if (cx1_len > cx2_len)
            {
                cx_len1_0 = cx2_len;
                return cx2;
            }
            else
                throw new NotImplementedException($"Cannot decide which intersection to pick.");
        }

        private static IEnumerable<Crossroad> getIntersections(GpxTrackSegment track1, GpxTrackSegment track2, Length limit)
        {
            for (int idx1 = 1; idx1 < track1.TrackPoints.Count; ++idx1)
            {
                Length len1 = Length.Zero;
                Length len1_ex = Length.Zero;

                for (int idx2_source = 1; idx2_source < track2.TrackPoints.Count; ++idx2_source)
                {
                    int idx2 = idx2_source;

                    IGeoPoint cx = getIntersection(track1.TrackPoints[idx1 - 1], track1.TrackPoints[idx1],
                        track2.TrackPoints[idx2 - 1], track2.TrackPoints[idx2],
                        out Length cx_len1_0);
                    if (cx == null)
                        continue;

                    if (len1 == Length.Zero)
                    {
                        len1 = track1.TrackPoints[idx1 - 1].GetDistance(track1.TrackPoints[idx1]);
                        len1_ex = len1 + limit;
                    }

                    // those check are basics -- if the interesection is too far away, it is not really an intersection of the tracks
                    if (cx_len1_0 > len1_ex)
                        continue;

                    Length cx_len1_1 = cx.GetDistance(track1.TrackPoints[idx1]);
                    if (cx_len1_1 > len1_ex)
                        continue;


                    Length len2 = track2.TrackPoints[idx2 - 1].GetDistance(track2.TrackPoints[idx2]);
                    Length len2_ex = len2 + limit;

                    Length cx_len2_0 = cx.GetDistance(track2.TrackPoints[idx2 - 1]);
                    if (cx_len2_0 > len2_ex)
                        continue;
                    Length cx_len2_1 = cx.GetDistance(track2.TrackPoints[idx2]);
                    if (cx_len2_1 > len2_ex)
                        continue;

                    var info = new Crossroad() { Point = cx };

                    // here we have to decide if we have a case of intersection like as "X"
                    // or if we have extensions of the tracks like "---- * -----"
                    // extension can only happen at the end or start of the track
                    Length diff1;
                    bool middle1 = false;
                    if (track1.TrackPoints.Count == 2)
                        diff1 = Length.Max(cx_len1_0, cx_len1_1) - len1;
                    else if (idx1 == 1)
                        diff1 = cx_len1_1 - len1;
                    else if (idx1 == track1.TrackPoints.Count - 1)
                        diff1 = cx_len1_0 - len1;
                    else
                    {
                        diff1 = Length.Zero;
                        middle1 = true;
                    }

                    Length diff2;
                    bool middle2 = false;
                    if (track2.TrackPoints.Count == 2)
                        diff2 = Length.Max(cx_len2_1, cx_len2_0) - len2;
                    else if (idx2 == 1)
                        diff2 = cx_len2_1 - len2;
                    else if (idx2 == track2.TrackPoints.Count - 1)
                        diff2 = cx_len2_0 - len2;
                    else
                    {
                        diff2 = Length.Zero;
                        middle2 = true;
                    }

                    if (middle1 || middle2)
                    {
                        // if we are in the middle of track we can get such shape "> * <" or "- *|"
                        // the overall distance cannot be too far...
                        if (cx_len1_0 + cx_len1_1 - len1 + cx_len2_0 + cx_len2_1 - len2 > limit)
                            continue;
                        // and we have to detect passing by "> * <", if in both cases crossing point is outside
                        // segment we have passing-by point
                        else if (cx_len1_0 + cx_len1_1 - len1 > numericAccuracy && cx_len2_0 + cx_len2_1 - len2 > numericAccuracy)
                            info.Kind = CrossroadKind.PassingBy;
                    }
                    // normally extension would look like: ---- x ----
                    // but we need to give ourselves a little slack, the track can be inaccurate
                    // so if we have intersection even within the segment but near the edge
                    // we still consider it as a extension, not "real" intersection
                    // we need 3 conditions to check the "slack" individually and as a sum
                    // so there will be no case that one track "borrows" too much slack from the limit
                    else if (diff1 > -limit && diff2 > -limit && diff1 + diff2 > -limit)
                    {
                        info.Kind = CrossroadKind.Extension;
                    }

                    yield return info;
                }
            }
        }

    }
}
