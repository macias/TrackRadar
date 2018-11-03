using Geo;
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
            await LoadGpxDataAsync(filename, out List<GpxTrackSegment> tracks, out List<IGeoPoint> waypoints).ConfigureAwait(false);

            List<List<NodePoint>> rich_tracks = tracks
                .Select(seg => seg.TrackPoints.Select((IGeoPoint p) => new NodePoint() { Point = p })
                .ToList()).ToList();

            IGeoMap<Segment> map = null;

            try
            {
                // add only distant intersections to user already marked waypoints
                IEnumerable<IGeoPoint> crossroads = filterDistant(waypoints,
                    findCrossroads(rich_tracks, offTrackDistance, out map),
                    offTrackDistance);
                waypoints.AddRange(crossroads);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }

            return new GpxData() { Crossroads = waypoints, Map = map };
        }

        public static Task LoadGpxDataAsync(string filename, out List<GpxTrackSegment> tracks, out List<IGeoPoint> waypoints)
        {
            tracks = new List<GpxTrackSegment>();
            waypoints = new List<IGeoPoint>();

            return loadGpxAsync(filename, tracks, waypoints);
        }

        private static async Task loadGpxAsync(string filename, List<GpxTrackSegment> tracks, List<IGeoPoint> waypoints)
        {
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
        }

        private static IEnumerable<IGeoPoint> filterDistant(IEnumerable<IGeoPoint> fixedPoints,
            IEnumerable<IGeoPoint> incoming, Length limit)
        {
            foreach (IGeoPoint pt in incoming)
                if (fixedPoints.All(it => it.GetDistance(pt) > limit))
                    yield return pt;
        }

        private static IEnumerable<IGeoPoint> findCrossroads(List<List<NodePoint>> tracks,
            Length offTrackDistance, out IGeoMap<Segment> map)
        {
            var crossroads = new List<Crossroad>();
            for (int i = 0; i < tracks.Count; ++i)
                for (int k = i + 1; k < tracks.Count; ++k)
                {
                    // mark each intersection with source index, this will allow us better averaging the points
                    crossroads.AddRange(getIntersections(tracks[i], tracks[k], offTrackDistance)
                        .Select(it => { it.SourceIndex = Tuple.Create(i, k); return it; }));
                }

            ;

            while (averageNearby(crossroads, offTrackDistance, onlySameTracks: true))
            {
                ;
            }

            ;

            while (averageNearby(crossroads, offTrackDistance, onlySameTracks: false))
            {
                ;
            }


            // do not export extensions of the tracks, only true intersections or passing by "> * <"
            // which we treat as almost-intersection
            removeExtensions(tracks, crossroads);

            map = buildMap(tracks);

            return crossroads.Select(it => it.Node.Point);
        }

        private static IGeoMap<Segment> buildMap(List<List<NodePoint>> tracks)
        {
            // extensions have to be removed at this point

            var visited = new HashSet<NodePoint>();
            var segments = new List<Segment>();

            foreach (List<NodePoint> trk in tracks)
            {
                for (int i = 0; i < trk.Count; ++i)
                {
                    NodePoint p = trk[i];

                    // segment as part of a track
                    if (i > 0)
                        segments.Add(new Segment(p.Point, trk[i - 1].Point));

                    // same point can come from two different tracks, because when they intersect
                    // the intersection point is added to both of them
                    if (visited.Add(p))
                    {
                        // segments coming from intersections
                        foreach (NodePoint neighbour in p.Neighbours)
                            if (!visited.Contains(neighbour))
                                segments.Add(new Segment(p.Point, neighbour.Point));
                    }
                }
            }

            return GeoMapFactory.Create(segments);
        }

        private static void removeExtensions(List<List<NodePoint>> tracks, List<Crossroad> crossroads)
        {
            var extensions = new HashSet<NodePoint>(crossroads
                .Where(it => it.Kind == CrossroadKind.Extension)
                .Select(it => it.Node));

            foreach (List<NodePoint> trk in tracks)
            {
                for (int i = trk.Count - 1; i >= 0; --i)
                {
                    NodePoint ext = trk[i];

                    // given point can be duplicated on few tracks (by definition of intersection)
                    if (extensions.Remove(ext))
                    {
                        ext.ConnectThrough();

                        foreach (NodePoint neighbour in ext.Neighbours)
                        {
                            if (i > 0)
                                neighbour.Connect(trk[i - 1]);
                            if (i < trk.Count - 1)
                                neighbour.Connect(trk[i + 1]);
                        }
                    }

                    // to remove extension from the track we need to check its kind directly
                    // (it might be removed from the set already)
                    if (ext.Kind == CrossroadKind.Extension)
                        trk.RemoveAt(i);
                }
            }

            foreach (NodePoint ext in extensions)
            {
                ext.ConnectThrough();
            }

            crossroads.RemoveAll(it => it.Kind == CrossroadKind.Extension);
        }

        private static bool averageNearby(List<Crossroad> crossroads, Length limit, bool onlySameTracks)
        {
            bool changed = false;

            for (int i = 0; i < crossroads.Count; ++i)
                for (int k = i + 1; k < crossroads.Count; ++k)
                {
                    Crossroad a = crossroads[i];
                    Crossroad b = crossroads[k];

                    bool same_tracks = Object.Equals(a.SourceIndex, b.SourceIndex) && a.SourceIndex != null;
                    if (onlySameTracks && !same_tracks)
                        continue;

                    if (GeoCalculator.GetDistance(a.Node.Point, b.Node.Point) < limit)
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

                        crossroads[i] = new Crossroad(a, b)
                        {
                            Kind = kind,
                            SourceIndex = same_tracks ? a.SourceIndex : null
                        };

                        crossroads[k] = crossroads[crossroads.Count - 1];
                        crossroads.RemoveAt(crossroads.Count - 1);

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

        private static bool computeDistances(IGeoPoint cx, List<NodePoint> track1, int idx1,
            List<NodePoint> track2, int idx2,
            Length len1_ex, ref Length len2_ex, Length limit,
            Length cx_len1_0, ref Length cx_len1_1, ref Length cx_len2_0, ref Length cx_len2_1)
        {
            cx_len1_0 = cx.GetDistance(track1[idx1 - 1].Point);
            // those check are basics -- if the interesection is too far away, it is not really an intersection of the tracks
            if (cx_len1_0 > len1_ex)
                return false;

            cx_len1_1 = cx.GetDistance(track1[idx1].Point);
            if (cx_len1_1 > len1_ex)
                return false;

            len2_ex = track2[idx2 - 1].Point.GetDistance(track2[idx2].Point) + limit;

            cx_len2_0 = cx.GetDistance(track2[idx2 - 1].Point);
            if (cx_len2_0 > len2_ex)
                return false;

            cx_len2_1 = cx.GetDistance(track2[idx2].Point);
            if (cx_len2_1 > len2_ex)
                return false;

            return true;
        }

        private static IEnumerable<Crossroad> getIntersections(List<NodePoint> track1,
            List<NodePoint> track2, Length limit)
        {
            for (int idx1 = 1; idx1 < track1.Count; ++idx1)
            {
                Length len1 = Length.Zero;
                Length len1_ex = Length.Zero;

                for (int idx2 = 1; idx2 < track2.Count; ++idx2)
                {
                    IGeoPoint cx = getIntersection(track1[idx1 - 1].Point, track1[idx1].Point,
                        track2[idx2 - 1].Point, track2[idx2].Point,
                        out Length cx_len1_0);
                    if (cx == null)
                        continue;

                    if (len1 == Length.Zero)
                    {
                        len1 = track1[idx1 - 1].Point.GetDistance(track1[idx1].Point);
                        len1_ex = len1 + limit;
                    }

                    Length len2_ex = Length.Zero;

                    Length cx_len1_1 = Length.Zero;
                    Length cx_len2_0 = Length.Zero;
                    Length cx_len2_1 = Length.Zero;

                    if (!computeDistances(cx, track1, idx1, track2, idx2, len1_ex, ref len2_ex, limit,
                        cx_len1_0, ref cx_len1_1, ref cx_len2_0, ref cx_len2_1))
                    {
                        // finding near intersection failed, but maybe it is caused by two almost parallel segments
                        // in such those two segments could be close to each other, but their intersection could be far away
                        // like in |\
                        // in such case we check distances between the endings of the segments

                        // on success we don't report intersection kind, because we don't have real one
                        bool middle1 = idx1 > 1 && idx1 < track1.Count - 1;
                        bool middle2 = idx2 > 1 && idx2 < track2.Count - 1;

                        Length track_dist;
                        track_dist = track1[idx1 - 1].Point.GetDistanceToArcSegment(track2[idx2 - 1].Point,
                            track2[idx2].Point, out cx);
                        if (track_dist < limit)
                        {
                            Crossroad crossroad = new Crossroad(cx)
                            {
                                Kind = middle1 || middle2 ? CrossroadKind.PassingBy : CrossroadKind.Extension,
                            }
                            .Connected(track1[idx1 - 1]);

                            yield return crossroad;

                            if (addInsertion(crossroad, track2, idx2))
                                ++idx2;
                        }
                        else
                        {
                            track_dist = track1[idx1].Point.GetDistanceToArcSegment(track2[idx2 - 1].Point,
                                track2[idx2].Point, out cx);
                            if (track_dist < limit)
                            {
                                Crossroad crossroad = new Crossroad(cx)
                                {
                                    Kind = middle1 || middle2 ? CrossroadKind.PassingBy : CrossroadKind.Extension,
                                }
                                .Connected(track1[idx1]);

                                yield return crossroad;

                                if (addInsertion(crossroad, track2, idx2))
                                    ++idx2;
                            }
                            else
                            {
                                track_dist = track2[idx2 - 1].Point.GetDistanceToArcSegment(track1[idx1 - 1].Point,
                                    track1[idx1].Point, out cx);
                                if (track_dist < limit)
                                {
                                    Crossroad crossroad = new Crossroad(cx)
                                    {
                                        Kind = middle1 || middle2 ? CrossroadKind.PassingBy : CrossroadKind.Extension,
                                    }
                                    .Connected(track2[idx2 - 1]);

                                    yield return crossroad;

                                    if (addInsertion(crossroad, track1, idx1))
                                    {
                                        ++idx1;
                                        len1 = Length.Zero;
                                        len1_ex = Length.Zero;
                                    }
                                }
                                else
                                {
                                    track_dist = track2[idx2].Point.GetDistanceToArcSegment(track1[idx1 - 1].Point,
                                        track1[idx1].Point, out cx);
                                    if (track_dist < limit)
                                    {
                                        Crossroad crossroad = new Crossroad(cx)
                                        {
                                            Kind = middle1 || middle2 ? CrossroadKind.PassingBy : CrossroadKind.Extension,
                                        }
                                        .Connected(track2[idx2]);

                                        yield return crossroad;

                                        if (addInsertion(crossroad, track1, idx1))
                                        {
                                            ++idx1;
                                            len1 = Length.Zero;
                                            len1_ex = Length.Zero;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Length len2 = len2_ex - limit;

                        var crossroad = new Crossroad(cx);

                        bool insertion = true;


                        // here we have to decide if we have a case of intersection like as "X"
                        // or if we have extensions of the tracks like "---- * -----"
                        // extension can only happen at the end or start of the track
                        Length diff1;
                        bool middle1 = false;
                        if (track1.Count == 2)
                            diff1 = Length.Max(cx_len1_0, cx_len1_1) - len1;
                        else if (idx1 == 1)
                            diff1 = cx_len1_1 - len1;
                        else if (idx1 == track1.Count - 1)
                            diff1 = cx_len1_0 - len1;
                        else
                        {
                            diff1 = Length.Zero;
                            middle1 = true;
                        }

                        Length diff2;
                        bool middle2 = false;
                        if (track2.Count == 2)
                            diff2 = Length.Max(cx_len2_1, cx_len2_0) - len2;
                        else if (idx2 == 1)
                            diff2 = cx_len2_1 - len2;
                        else if (idx2 == track2.Count - 1)
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
                            // and we have to detect passing-by "> * <", if in both cases crossing point is outside
                            // segment we have passing-by point
                            else if (cx_len1_0 + cx_len1_1 - len1 > numericAccuracy && cx_len2_0 + cx_len2_1 - len2 > numericAccuracy)
                            {
                                crossroad.Kind = CrossroadKind.PassingBy;
                                insertion = false;
                                crossroad.Connected(cx_len1_0 > len1 ? track1[idx1] : track1[idx1 - 1]);
                                crossroad.Connected(cx_len2_0 > len2 ? track2[idx2] : track2[idx2 - 1]);
                            }
                        }
                        // normally extension would look like: ---- x ----
                        // but we need to give ourselves a little slack, the track can be inaccurate
                        // so if we have intersection even within the segment but near the edge
                        // we still consider it as a extension, not "real" intersection
                        // we need 3 conditions to check the "slack" individually and as a sum
                        // so there will be no case that one track "borrows" too much slack from the limit
                        else if (diff1 > -limit && diff2 > -limit && diff1 + diff2 > -limit)
                        {
                            insertion = false;
                            crossroad.Kind = CrossroadKind.Extension;
                            crossroad.Connected(cx_len1_0 > len1 ? track1[idx1] : track1[idx1 - 1]);
                            crossroad.Connected(cx_len2_0 > len2 ? track2[idx2] : track2[idx2 - 1]);
                        }

                        yield return crossroad;

                        if (insertion)
                        {
                            if (addInsertion(crossroad, track2, idx2))
                                ++idx2;
                            if (addInsertion(crossroad, track1, idx1))
                            {
                                ++idx1;
                                len1 = Length.Zero;
                                len1_ex = Length.Zero;
                            }
                        }
                    }
                }
            }
        }

        private static bool addInsertion(Crossroad crossroad, List<NodePoint> track, int idx)
        {
            // check against the case we have insertion point right at the beginning/end of the segment
            if (track[idx].Point == crossroad.Node.Point || track[idx - 1].Point == crossroad.Node.Point)
                return false;

            track.Insert(idx, crossroad.Node);
            crossroad.RegisterInsertion(track);
            return true;
        }
    }
}
