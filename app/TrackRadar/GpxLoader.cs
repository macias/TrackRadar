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

        public static IPlanData ReadGpx(string filename, Length offTrackDistance,
            Action<double> onProgress,
            CancellationToken token)
        {
            if (!tryLoadGpx(filename, out List<List<GeoPoint>> tracks, out List<GeoPoint> waypoints,
                x => onProgress?.Invoke(x / 2), token))
                return null;

            if (token.IsCancellationRequested)
                return null;

            return ProcessTrackData(tracks, waypoints, offTrackDistance, segmentLengthLimit: GeoMapFactory.SegmentLengthLimit, onProgress, token);
        }

        internal static IPlanData ProcessTrackData(IEnumerable<IEnumerable<GeoPoint>> tracks, IEnumerable<GeoPoint> waypoints,
            Length offTrackDistance, Length segmentLengthLimit, Action<double> onProgress, CancellationToken token)
        {
            var waypoints_list = waypoints.Distinct().ToList();
            var tracks_list = tracks.Select(it => new Track(it)).ToList();

            // add only distant intersections to user already marked waypoints
            if (!tryFindCrossroads(tracks_list, offTrackDistance, x => onProgress?.Invoke(x / 2 + 0.5),
                out IEnumerable<Crossroad> crossroads_found, token))
                return null;

            crossroads_found = getDistant(waypoints_list, crossroads_found, offTrackDistance).ToArray();

            var turn_graph = createTurnGraph(tracks_list, waypoints_list, crossroads_found,
                offTrackDistance, segmentLengthLimit: segmentLengthLimit);

            waypoints_list.AddRange(crossroads_found.Select(it => it.Point));

            // extensions have to be removed at this point
            IEnumerable<ISegment> segments = tracks_list.SelectMany(trk => trk.Nodes.Where(it => !it.IsLast)).ToArray();

            return new PlanData(
                segments,
                waypoints_list,
                // do not pass crossroads as track points because when someone created track manually, 
                // crossroad can be off the track slightly
                turn_graph
                );
        }

        internal static bool tryLoadGpx(string filename, out List<List<GeoPoint>> tracks,
            out List<GeoPoint> waypoints,
            Action<double> onProgress,
            CancellationToken token)
        {
            tracks = new List<List<GeoPoint>>();
            waypoints = new List<GeoPoint>();
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
                            if (reader.WayPoint.Name?.StartsWith("-") ?? false)
                            {
                                ; // hack, treat such point only as display/visual hint, do not use it for navigation
                            }
                            else
                            {
                                waypoints.Add(GpxHelper.FromGpx(reader.WayPoint));
                            }
                            break;
                        case GpxObjectType.Route:
                            break;
                        case GpxObjectType.Track:
                            tracks.AddRange(reader.Track.Segments
                                .Select(trk => trk.TrackPoints.Select(p => GpxHelper.FromGpx(p)).ToList()));
                            break;
                    }
                }
            }

            return true;
        }

        private static IEnumerable<Crossroad> getDistant(IEnumerable<GeoPoint> fixedPoints,
            IEnumerable<Crossroad> incoming, Length limit)
        {
            foreach (Crossroad cx in incoming)
                if (fixedPoints.All(it => GeoCalculator.GetDistance(it, cx.Point) > limit))
                    yield return cx;
        }

        private static bool tryFindCrossroads(List<Track> tracks,
            Length offTrackDistance, Action<double> onProgress,
            out IEnumerable<Crossroad> crossroadsFound,
            CancellationToken token)
        {
            var crossroads = new List<GpxLoader.Crossroad>();
            crossroadsFound = crossroads;

            {
                int total_steps = (tracks.Count - 1) * tracks.Count / 2;
                int step = 0;
                for (int i = 0; i < tracks.Count; ++i)
                    for (int k = i + 1; k < tracks.Count; ++k, ++step)
                    {
                        if (token.IsCancellationRequested)
                            return false;

                        onProgress?.Invoke(step * 1.0 / total_steps);

                        // mark each intersection with source index, this will allow us better averaging the points
                        IEnumerable<Crossroad> intersections = getTrackIntersections(tracks[i], tracks[k], offTrackDistance);
                        // at this point each track can be enriched with connection into to this or that crossroad
                        crossroads.AddRange(intersections.Select(it => { it.SetSourceIndex(i, k); return it; }));

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
            crossroads.RemoveAll(it => it.Kind == GpxLoader.CrossroadKind.Extension);

            if (token.IsCancellationRequested)
                return false;

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

        public static void WriteGpxSegments(string path, IEnumerable<ISegment> segments)
        {
            using (var file = new GpxDirtyWriter(path))
            {
                int count = 0;
                foreach (ISegment s in segments)
                    file.WriteTrack($"seg{count++}", s.A, s.B);
            }
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
                .Select(it => it.Point)
                .ToArray();

            for (int i = crossroads.Count - 1; i >= 0; --i)
            {
                GpxLoader.Crossroad passing_by = crossroads[i];

                if (passing_by.Kind != GpxLoader.CrossroadKind.PassingBy)
                    continue;

                foreach (GeoPoint pt in solid_points)
                {
                    if (GeoCalculator.GetDistance(passing_by.Point, pt) < limit)
                    {
                        passing_by.Clear();
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

                    if (GeoCalculator.GetDistance(a.Point, b.Point) < limit)
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

                        crossroads[i] = GpxLoader.Crossroad.AverageCrossroads(a, b, kind, same_tracks ? a.SourceIndex : null);

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
            Length sig_cx2_len = GeoCalculator.EarthOppositePointSignedDistance(sig_cx1_len);

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

        private static IEnumerable<GpxLoader.Crossroad> getTrackIntersections(Track track1,
            Track track2, Length limit)
        {
            int __idx1 = 0;
            foreach (TrackNode node1_0 in track1.Nodes.Where(it => !it.IsLast))
            {
                ++__idx1;

                Length len1 = Length.Zero;

                TrackNode node1_1 = node1_0.Next;

                Length sig_len1 = GeoCalculator.GetSignedDistance(node1_0.Point, node1_1.Point);

                int __idx2 = 0;
                foreach (TrackNode node2_0 in track2.Nodes.Where(it => !it.IsLast))
                {
                    ++__idx2;

                    TrackNode node2_1 = node2_0.Next;

                    if (!TryGetSegmentIntersection(node1_0.Point, node1_1.Point,
                        node2_0.Point, node2_1.Point,
                        out Length cx_len1_0, out GeoPoint cx))
                        continue;

                    len1 = sig_len1.Abs();

                    Length sig_len2 = Length.Zero;

                    Length cx_len1_1 = Length.Zero;
                    Length cx_len2_0 = Length.Zero;
                    Length cx_len2_1 = Length.Zero;

                    bool is_nearby = ComputeDistances(cx, node1_1.Point, node2_0.Point, node2_1.Point,
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
                        bool middle1 = !node1_0.IsFirst && !node1_1.IsLast;
                        bool middle2 = !node2_0.IsFirst && !node2_1.IsLast;

                        Length track_track_dist;
                        track_track_dist = node1_0.Point
                            .GetDistanceToArcSegment(node2_0.Point, node2_1.Point, out cx);
                        if (track_track_dist < limit)
                        {
                            var mid_cx = GeoCalculator.GetMidPoint(cx, node1_0.Point);
                            GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(mid_cx,
                                middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension)
                                // todo: extra computation
                                .Connected(node1_0, null)
                                .Projected(node2_0, cx)
                                ;

                            yield return crossroad;
                        }
                        else
                        {
                            track_track_dist = node1_1.Point
                                .GetDistanceToArcSegment(node2_0.Point, node2_1.Point, out cx);
                            if (track_track_dist < limit)
                            {
                                var mid_cx = GeoCalculator.GetMidPoint(cx, node1_1.Point);
                                GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(mid_cx,
                                    middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension)
                                // todo: extra computation
                                .Connected(node1_1, null)
                                .Projected(node2_0, cx)
                                ;

                                yield return crossroad;
                            }
                            else
                            {
                                track_track_dist = node2_0.Point
                                    .GetDistanceToArcSegment(node1_0.Point, node1_1.Point, out cx);
                                if (track_track_dist < limit)
                                {
                                    var mid_cx = GeoCalculator.GetMidPoint(cx, node2_0.Point);
                                    GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(mid_cx,
                                        middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension)
                                    // todo: extra computation
                                    .Connected(node2_0, null)
                                    .Projected(node1_0, cx)
                                    ;

                                    yield return crossroad;
                                }
                                else
                                {
                                    track_track_dist = node2_1.Point
                                        .GetDistanceToArcSegment(node1_0.Point, node1_1.Point, out cx);
                                    if (track_track_dist < limit)
                                    {
                                        var mid_cx = GeoCalculator.GetMidPoint(cx, node2_1.Point);
                                        GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(mid_cx,
                                            middle1 || middle2 ? GpxLoader.CrossroadKind.PassingBy : GpxLoader.CrossroadKind.Extension)
                                        // todo: extra computation
                                        .Connected(node2_1, null)
                                        .Projected(node1_0, cx)
                                        ;

                                        yield return crossroad;
                                    }
                                }
                            }
                        }
                    }
                    else // we have actual intersection (at least within limits)
                    {
                        Length len2 = sig_len2.Abs();

                        // here we have to decide if we have a case of intersection like as "X"
                        // or if we have extensions of the tracks like "---- * -----"
                        // extension can only happen at the end or start of the track

                        Length diff1;
                        bool in_middle_track1 = false;

                        if (node1_0.IsFirst && node1_1.IsLast)
                            diff1 = Length.Max(cx_len1_0, cx_len1_1) - len1;
                        else if (__idx1 == 1)
                            diff1 = cx_len1_1 - len1;
                        else if (node1_1.IsLast)
                            diff1 = cx_len1_0 - len1;
                        else
                        {
                            diff1 = Length.Zero;
                            in_middle_track1 = true;
                        }

                        Length diff2;
                        bool in_middle_track2 = false;

                        if (node2_0.IsFirst && node2_1.IsLast)
                            diff2 = Length.Max(cx_len2_1, cx_len2_0) - len2;
                        else if (__idx2 == 1)
                            diff2 = cx_len2_1 - len2;
                        else if (node2_1.IsLast)
                            diff2 = cx_len2_0 - len2;
                        else
                        {
                            diff2 = Length.Zero;
                            in_middle_track2 = true;
                        }

                        Crossroad crossroad;

                        if (in_middle_track1 || in_middle_track2)
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
                                crossroad = new Crossroad(cx, GpxLoader.CrossroadKind.PassingBy);
                                {
                                    bool second = len1 < cx_len1_0;
                                    crossroad.Connected(second ? node1_1 : node1_0, second ? cx_len1_1 : cx_len1_0);
                                }
                                {
                                    bool second = len2 < cx_len2_0;
                                    crossroad.Connected(second ? node2_1 : node2_0, second ? cx_len2_1 : cx_len2_0);
                                }
                            }
                            else
                            {
                                crossroad = new GpxLoader.Crossroad(cx, CrossroadKind.Intersection)
                                    .Projected(node1_0, cx, cx_len1_0)
                                    .Projected(node2_0, cx, cx_len2_0)
                                    ;
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
                            crossroad = new Crossroad(cx, GpxLoader.CrossroadKind.Extension);
                            {
                                bool first = cx_len1_0 > len1;
                                crossroad.Connected(first ? node1_1 : node1_0, first ? cx_len1_1 : cx_len1_0);
                            }
                            {
                                bool first = cx_len2_0 > len2;
                                crossroad.Connected(first ? node2_1 : node2_0, first ? cx_len2_1 : cx_len2_0);
                            }
                        }
                        else
                        {
                            crossroad = new GpxLoader.Crossroad(cx, CrossroadKind.Intersection)
                                    .Projected(node1_0, cx, cx_len1_0)
                                    .Projected(node2_0, cx, cx_len2_0)
                                    ;
                        }


                        yield return crossroad;
                    }
                }
            }
        }
    }
}
