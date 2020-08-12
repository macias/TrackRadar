using Geo;
using Gpx;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public sealed partial class GpxLoader
    {
        private static readonly Length numericAccuracy = Length.FromMeters(1);

        /*public static GpxData ReadBasicGpx(string filename)
        {
            LoadGpxData(filename, out List<GpxTrackSegment> tracks, out List<IGpxPoint> waypoints);

            var segments = new List<Segment>();
            foreach (var track in tracks)
                for (int i = 1; i < track.TrackPoints.Count; ++i)
                    segments.Add(new Segment(GeoPoint.FromGpx(track.TrackPoints[i - 1]), GeoPoint.FromGpx(track.TrackPoints[i])));

            return new GpxData(segments, waypoints);
        }*/

        public static GpxData ReadGpx(string filename, Length offTrackDistance,
            Action<double> onProgress,
            CancellationToken token)
        {
            if (!TryLoadGpxData(filename, out List<List<GpxLoader.NodePoint>> tracks, out List<GeoPoint> waypoints, 
                x => onProgress?.Invoke(x/2), token))
                return null;

            // todo: bug -- this method swaps segments A-B into B-A
            // List<List<GpxLoader.NodePoint>> rich_tracks = tracks
            //   .Select(seg => seg.TrackPoints.Select((GpxTrackPoint p) => new GpxLoader.NodePoint() { Point = GeoPoint.FromGpx(p) })
            // .ToList()).ToList();

            if (token.IsCancellationRequested)
                return null;

            IEnumerable<Segment> segments = null;
            //Dictionary<GeoPoint, Turn> turn_info = null;

            //List<XGeoPoint> waypoints = gpx_waypoints.Select(it => GeoPoint.FromGpx(it)).ToList();

            // add only distant intersections to user already marked waypoints
            if (!tryFindCrossroads(tracks, offTrackDistance, x=> onProgress?.Invoke(x/2+0.5), out segments, out IEnumerable<GeoPoint> crossroads_found, token))
                return null;

            waypoints.AddRange(filterDistant(waypoints, crossroads_found, offTrackDistance));

            //  turn_info = computeTurns(segments, waypoints, turnAheadDistance);

            return new GpxData(segments, waypoints);//, turn_info);
        }
        /*
        internal static Dictionary<GeoPoint, Turn> computeTurns(IEnumerable<Segment> segments, IEnumerable<GeoPoint> crossroads,
            Length turnAheadDistance)
        {
            var turn_info = new Dictionary<GeoPoint, Turn>();

            foreach (GeoPoint cx in crossroads)
            {
                if (tryComputeTurn(cx, segments, turnAheadDistance, out Turn turn))
                    turn_info.Add(cx, turn);
            }

            return turn_info;
        }*/



        internal static bool TryLoadGpxData(string filename, out List<List<GpxLoader.NodePoint>> tracks, out List<GeoPoint> waypoints,
            Action<double> onProgress,
            CancellationToken token)
        {
            tracks = new List<List<GpxLoader.NodePoint>>();
            waypoints = new List<GeoPoint>();

            return tryLoadGpx(filename, tracks, waypoints, onProgress, token);
        }

        private static bool tryLoadGpx(string filename, List<List<GpxLoader.NodePoint>> tracks, List<GeoPoint> waypoints,
            Action<double> onProgress,
            CancellationToken token)
        {
            using (GpxIOFactory.CreateReader(filename, out IGpxReader reader, out IStreamProgress stream_progress))
            {
                while (reader.Read(out GpxObjectType type))
                {
                    onProgress?.Invoke(stream_progress.Position * 1.0 / stream_progress.Length);

                    if (token.IsCancellationRequested)
                        return false;

                    switch (type)
                    {
                        case GpxObjectType.Metadata:
                            break;
                        case GpxObjectType.WayPoint:
                            waypoints.Add(GpxHelper.FromGpx(reader.WayPoint));
                            break;
                        case GpxObjectType.Route:
                            break;
                        case GpxObjectType.Track:
                            // todo: bug -- this method swaps segments A-B into B-A
                            tracks.AddRange(reader.Track.Segments.Select(trk => trk.TrackPoints
                                .Select(p => new GpxLoader.NodePoint() { Point = GpxHelper.FromGpx(p) }).ToList()));
                            break;
                    }
                }
            }

            return true;
        }

        private static IEnumerable<GeoPoint> filterDistant(IEnumerable<GeoPoint> fixedPoints,
            IEnumerable<GeoPoint> incoming, Length limit)
        {
            foreach (GeoPoint pt in incoming)
                if (fixedPoints.All(it => GeoCalculator.GetDistance(it, pt) > limit))
                    yield return pt;
        }

        private static bool tryFindCrossroads(List<List<GpxLoader.NodePoint>> tracks,
            Length offTrackDistance,Action<double> onProgress, out IEnumerable<Segment> segments, out IEnumerable<GeoPoint> crossroadsFound,
            CancellationToken token)
        {
            segments = null;
            crossroadsFound = null;

            var crossroads = new List<GpxLoader.Crossroad>();
            {
                int total_steps = (tracks.Count - 1) * tracks.Count / 2;
                int step = 0;
                for (int i = 0; i < tracks.Count; ++i)
                    for (int k = i + 1; k < tracks.Count; ++k,++step)
                    {
                        if (token.IsCancellationRequested)
                            return false;

                        onProgress?.Invoke(step * 1.0 / total_steps);

                        // mark each intersection with source index, this will allow us better averaging the points
                        IEnumerable<Crossroad> intersections = getTrackIntersections(tracks[i], tracks[k], offTrackDistance);
                        crossroads.AddRange(intersections.Select(it => { it.SourceIndex = new GpxLoader.TrackIndex(i, k); return it; }));

                    }
            }

            if (token.IsCancellationRequested)
                return false;

            removePassingBy(crossroads, offTrackDistance * 2);

            if (token.IsCancellationRequested)
                return false;

            while (averageNearby(crossroads, offTrackDistance, onlySameTracks: true))
            {
                if (token.IsCancellationRequested)
                    return false;
            }


            while (averageNearby(crossroads, offTrackDistance, onlySameTracks: false))
            {
                if (token.IsCancellationRequested)
                    return false;
            }

            if (token.IsCancellationRequested)
                return false;

            // do not export extensions of the tracks, only true intersections or passing by "> * <"
            // which we treat as almost-intersection
            removeExtensions(tracks, crossroads);

            if (token.IsCancellationRequested)
                return false;

            segments = buildTrackSegments(tracks);

            crossroadsFound = crossroads.Select(it => it.Node.Point);

            return true;
        }

        public static void WriteGpxPoints(string path, IEnumerable<GeoPoint> points)
        {
            using (var file = new GpxDirtyWriter(path))
            {
                foreach (var p in points)
                    file.WritePoint(p, null);
            }
        }

        public static void WriteGpxSegments(string path, IEnumerable<Segment> segments)
        {
            using (var file = new GpxDirtyWriter(path))
            {
                int count = 0;
                foreach (Segment s in segments)
                    file.WriteTrack($"seg{count++}", s.A, s.B);
            }
        }

        private static IEnumerable<Segment> buildTrackSegments(IReadOnlyList<IReadOnlyList<GpxLoader.NodePoint>> tracks)
        {
            // extensions have to be removed at this point

            var visited = new HashSet<GpxLoader.NodePoint>();
            var segments = new List<Segment>();

            foreach (List<GpxLoader.NodePoint> trk in tracks)
            {
                for (int i = 0; i < trk.Count; ++i)
                {
                    GpxLoader.NodePoint p = trk[i];

                    // segment as part of a track
                    if (i > 0)
                        segments.Add(new Segment(trk[i - 1].Point, p.Point));

                    // same point can come from two different tracks, because when they intersect
                    // the intersection point is added to both of them
                    if (false && visited.Add(p))
                    {
                        // segments coming from intersections
                        foreach (GpxLoader.NodePoint neighbour in p.Neighbours)
                            if (!visited.Contains(neighbour))
                                segments.Add(new Segment(p.Point, neighbour.Point));
                    }
                }
            }

            return segments;
        }

        private static void removeExtensions(List<List<GpxLoader.NodePoint>> tracks, List<GpxLoader.Crossroad> crossroads)
        {
            var extensions = new HashSet<GpxLoader.NodePoint>(crossroads
                .Where(it => it.Kind == GpxLoader.CrossroadKind.Extension)
                .Select(it => it.Node));

            foreach (List<GpxLoader.NodePoint> trk in tracks)
            {
                for (int i = trk.Count - 1; i >= 0; --i)
                {
                    GpxLoader.NodePoint ext = trk[i];

                    // given point can be duplicated on few tracks (by definition of intersection)
                    if (extensions.Remove(ext))
                    {
                        ext.ConnectThrough();

                        foreach (GpxLoader.NodePoint neighbour in ext.Neighbours)
                        {
                            if (i > 0)
                                neighbour.Connect(trk[i - 1]);
                            if (i < trk.Count - 1)
                                neighbour.Connect(trk[i + 1]);
                        }
                    }

                    // to remove extension from the track we need to check its kind directly
                    // (it might be removed from the set already)
                    if (ext.Kind == GpxLoader.CrossroadKind.Extension)
                        trk.RemoveAt(i);
                }
            }

            foreach (GpxLoader.NodePoint ext in extensions)
            {
                ext.ConnectThrough();
            }

            crossroads.RemoveAll(it => it.Kind == GpxLoader.CrossroadKind.Extension);
        }


        private static void removePassingBy(List<GpxLoader.Crossroad> crossroads, Length limit)
        {
            // this is weak but it was easy to implement and so far it works

            // PROBLEM: some segment will give us "passing by", this is ok, but the following segments
            // will give us intersection or extension. In such case we would like to invalidate
            // the "passing by" point, and the problem is such point can lie in a distance from the intersection/extension

            // IDEA: we could store cross point with info about segments points, this way we could check if passing-by
            // point lies near intersection/extension or its originating segment, but this means 5 computations per
            // one check

            // HACK: simply use bigger distance limit when removing passing-by points and compare just against intersection/extension

            var solid_points = crossroads
                .Where(it => it.Kind != GpxLoader.CrossroadKind.PassingBy)
                .Select(it => it.Node)
                .ToArray();

            for (int i = crossroads.Count - 1; i >= 0; --i)
            {
                GpxLoader.Crossroad passing_by = crossroads[i];

                if (passing_by.Kind != GpxLoader.CrossroadKind.PassingBy)
                    continue;

                foreach (GpxLoader.NodePoint np in solid_points)
                {
                    if (GeoCalculator.GetDistance(passing_by.Node.Point, np.Point) < limit)
                    {
                        passing_by.Disconnect();
                        crossroads.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private static bool averageNearby(List<GpxLoader.Crossroad> crossroads, Length limit, bool onlySameTracks)
        {
            bool changed = false;

            for (int i = 0; i < crossroads.Count; ++i)
                for (int k = i + 1; k < crossroads.Count; ++k)
                {
                    GpxLoader.Crossroad a = crossroads[i];
                    GpxLoader.Crossroad b = crossroads[k];

                    bool same_tracks = Object.Equals(a.SourceIndex, b.SourceIndex) && a.SourceIndex != null;
                    if (onlySameTracks && !same_tracks)
                        continue;

                    if (GeoCalculator.GetDistance(a.Node.Point, b.Node.Point) < limit)
                    {
                        GpxLoader.CrossroadKind kind = GpxLoader.CrossroadKind.Intersection;
                        if (same_tracks)// && a.Kind != GpxLoader.CrossroadKind.Intersection && b.Kind != GpxLoader.CrossroadKind.Intersection)
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
                            if (a.Kind == GpxLoader.CrossroadKind.Extension || b.Kind == GpxLoader.CrossroadKind.Extension)
                                kind = GpxLoader.CrossroadKind.Extension;
                            // also we have to deal with passing-by points like in shape "> * <"
                            // this is actually "else" case but we add this condition as sanity check
                            else if (a.Kind == GpxLoader.CrossroadKind.PassingBy && b.Kind == GpxLoader.CrossroadKind.PassingBy)
                                kind = GpxLoader.CrossroadKind.PassingBy;
                            //else
                            //  throw new NotImplementedException($"This case is not possible, right? {a.Kind}, {b.Kind}");
                        }

                        crossroads[i] = new GpxLoader.Crossroad(a, b)
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

        internal static bool TryGetSegmentIntersection(in GeoPoint point10, in GeoPoint point11,
            in GeoPoint point20, in GeoPoint point21, out Length sig_cx_len1_0, out GeoPoint result)
        {
            GeoPoint cx1;
            if (!GeoCalculator.TryGetArcIntersection(point10, point11, point20, point21, out cx1))
            {
                sig_cx_len1_0 = Length.Zero;
                result = default;
                return false;
            }

            Length sig_cx1_len = GeoCalculator.GetSignedDistance(cx1, point10);
            Length sig_cx2_len = GeoCalculator.OppositePointSignedDistance(sig_cx1_len);

            if (sig_cx1_len.Abs() < sig_cx2_len.Abs())
            {
                sig_cx_len1_0 = sig_cx1_len;
                result = cx1;
                return true;
            }
            else if (sig_cx1_len.Abs() > sig_cx2_len.Abs())
            {
                sig_cx_len1_0 = sig_cx2_len;
                result = GeoCalculator.OppositePoint(cx1);
                return true;
            }
            else
                throw new NotImplementedException($"Cannot decide which intersection to pick.");
        }


        private static bool isWithinLimit(Length sig_len, Length segment_len, Length limit)
        {
            if (sig_len.Sign() == segment_len.Sign())
            {
                return sig_len.Abs() <= segment_len.Abs() + limit;
            }
            else
            {
                return sig_len.Abs() <= limit;
            }
        }

        internal static bool ComputeDistances(in GeoPoint cx,
            in GeoPoint point11,
            //IReadOnlyList<GpxLoader.NodePoint> track1, int idx1,
            //IReadOnlyList<GpxLoader.NodePoint> track2, int idx2,
            in GeoPoint point20, in GeoPoint point21,
            Length sig_len1, ref Length sig_len2, Length limit,
            Length cx_len1_0, ref Length cx_len1_1, ref Length cx_len2_0, ref Length cx_len2_1)
        {
            // those check are basics -- if the interesection is too far away, 
            // it is not really an intersection of the tracks
            if (!isWithinLimit(-cx_len1_0, sig_len1, limit))
                return false;

            // todo: we should be able to compute the distance by simple subtracting
            cx_len1_1 = GeoCalculator.GetSignedDistance(cx, point11);

            sig_len2 = GeoCalculator.GetSignedDistance(point20, point21);

            cx_len2_0 = GeoCalculator.GetSignedDistance(cx, point20);
            if (!isWithinLimit(-cx_len2_0, sig_len2, limit))
                return false;

            cx_len2_1 = GeoCalculator.GetSignedDistance(cx, point21);

            return true;
        }

        private static IEnumerable<GpxLoader.Crossroad> getTrackIntersections(IReadOnlyList<GpxLoader.NodePoint> track1,
            IReadOnlyList<GpxLoader.NodePoint> track2, Length limit)
        {
            for (int idx1 = 1; idx1 < track1.Count; ++idx1)
            {
                Length len1 = Length.Zero;

                GpxLoader.NodePoint node10 = track1[idx1 - 1];
                GpxLoader.NodePoint node11 = track1[idx1];
                Length sig_len1 = GeoCalculator.GetSignedDistance(node10.Point, node11.Point);

                for (int idx2 = 1; idx2 < track2.Count; ++idx2)
                {

                    GpxLoader.NodePoint node20 = track2[idx2 - 1];
                    GpxLoader.NodePoint node21 = track2[idx2];
                    if (!TryGetSegmentIntersection(node10.Point, node11.Point,
                        node20.Point, node21.Point,
                        out Length cx_len1_0, out GeoPoint cx))
                        continue;

                    len1 = sig_len1.Abs();

                    Length sig_len2 = Length.Zero;

                    Length cx_len1_1 = Length.Zero;
                    Length cx_len2_0 = Length.Zero;
                    Length cx_len2_1 = Length.Zero;

                    bool is_nearby = ComputeDistances(cx, node11.Point, node20.Point, node21.Point,
                        sig_len1, ref sig_len2, limit,
                        cx_len1_0, ref cx_len1_1, ref cx_len2_0, ref cx_len2_1);

                    cx_len1_0 = cx_len1_0.Abs();
                    cx_len1_1 = cx_len1_1.Abs();
                    cx_len2_0 = cx_len2_0.Abs();
                    cx_len2_1 = cx_len2_1.Abs();

                    if (!is_nearby)
                    {
                        // finding near intersection failed, but maybe it is caused by two almost parallel segments
                        // in such those two segments could be close to each other, but their intersection could be far away
                        // like in |\
                        // in such case we check distances between the endings of the segments

                        // on success we don't report intersection kind, because we don't have real one
                        bool middle1 = idx1 > 1 && idx1 < track1.Count - 1;
                        bool middle2 = idx2 > 1 && idx2 < track2.Count - 1;

                        Length track_track_dist;
                        track_track_dist = node10.Point
                            .GetDistanceToArcSegment(node20.Point, node21.Point, out cx);
                        if (track_track_dist < limit)
                        {
                            GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(cx)
                            {
                                Kind = middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension,
                            }
                            .Connected(node10)
                            ;

                            yield return crossroad;

                            //   if (addInsertion(crossroad, track2, idx2))
                            //     ++idx2;
                        }
                        else
                        {
                            track_track_dist = node11.Point
                                .GetDistanceToArcSegment(node20.Point, node21.Point, out cx);
                            if (track_track_dist < limit)
                            {
                                GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(cx)
                                {
                                    Kind = middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension,
                                }
                                .Connected(node11);

                                yield return crossroad;

                                //  if (addInsertion(crossroad, track2, idx2))
                                //    ++idx2;
                            }
                            else
                            {
                                track_track_dist = node20.Point
                                    .GetDistanceToArcSegment(node10.Point, node11.Point, out cx);
                                if (track_track_dist < limit)
                                {
                                    GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(cx)
                                    {
                                        Kind = middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension,
                                    }
                                    .Connected(node20);

                                    yield return crossroad;

                                    /*  if (addInsertion(crossroad, track1, idx1))
                                      {
                                          ++idx1;
                                          len1 = Length.Zero;
                                      }*/
                                }
                                else
                                {
                                    track_track_dist = node21.Point
                                        .GetDistanceToArcSegment(node10.Point, node11.Point, out cx);
                                    if (track_track_dist < limit)
                                    {
                                        GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(cx)
                                        {
                                            Kind = middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension,
                                        }
                                        .Connected(node21);

                                        yield return crossroad;

                                        /* if (addInsertion(crossroad, track1, idx1))
                                         {
                                             ++idx1;
                                             len1 = Length.Zero;
                                         }*/
                                    }
                                }
                            }
                        }
                    }
                    else // we have near intersection
                    {
                        Length len2 = sig_len2.Abs();

                        var crossroad = new GpxLoader.Crossroad(cx);

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
                            // we divide by "2" because when we are in the middle of the segment the difference will be zero
                            // but when we are outside without division we would count extension twice (from each of the points)
                            Length len1_diff = (cx_len1_0 + cx_len1_1 - len1) / 2;
                            Length len2_diff = (cx_len2_0 + cx_len2_1 - len2) / 2;
                            Length diff_total = len1_diff + len2_diff;
                            if (diff_total > limit)
                                continue;
                            // and we have to detect passing-by "> * <", if in both cases crossing point is outside
                            // segment we have passing-by point
                            else if (len1_diff > numericAccuracy && len2_diff > numericAccuracy)
                            {
                                crossroad.Kind = GpxLoader.CrossroadKind.PassingBy;
                                crossroad.Connected(cx_len1_0 > len1 ? node11 : node10);
                                crossroad.Connected(cx_len2_0 > len2 ? node21 : node20);
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
                            crossroad.Kind = GpxLoader.CrossroadKind.Extension;
                            crossroad.Connected(cx_len1_0 > len1 ? node11 : node10);
                            crossroad.Connected(cx_len2_0 > len2 ? node21 : node20);
                        }

                        yield return crossroad;
                    }
                }
            }
        }
    }
}
