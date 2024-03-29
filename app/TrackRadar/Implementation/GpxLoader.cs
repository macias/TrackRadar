﻿using Geo;
using Gpx;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using TrackRadar.Implementation;

[assembly: InternalsVisibleTo("TestRunner")]

namespace TrackRadar.Implementation
{
    public sealed partial class GpxLoader
    {
        private static readonly Length numericAccuracy = Length.FromMeters(1);

        public static double RecomputeProgress(Stage stage, long step, long total)
        {
            return ((int)stage + step * 1.0 / total) / StageCount;
        }
        public static IPlanData ReadGpx(
#if DEBUG
            MetaLogger DEBUG_logger,
#endif
            string filename, Length offTrackDistance,
            Action<Stage, long, long> onProgress,
            CancellationToken token)
        {
            if (!tryLoadGpx(filename, out List<List<GeoPoint>> tracks, out List<(GeoPoint point, WayPointKind kind)> waypoints,
                (step, total) => onProgress?.Invoke(Stage.Loading, step, total), token))
                return null;

            if (token.IsCancellationRequested)
                return null;

            return processTrackData(
#if DEBUG
                DEBUG_logger,
#endif
                tracks, waypoints, offTrackDistance, segmentLengthLimit: GeoMapFactory.SegmentLengthLimit,
                onProgress, token);
        }

#if DEBUG
        // another sick hack for ValueTuple and problems with resolving them in test projects
        internal static IPlanData ProcessTrackData(
            MetaLogger DEBUG_logger,
            IEnumerable<IReadOnlyList<GeoPoint>> tracks,
    IEnumerable<GeoPoint> waypoints, IEnumerable<GeoPoint> endpoints,
    Length offTrackDistance, Length segmentLengthLimit, Action<Stage, long, long> onProgress, CancellationToken token)
        {
            return processTrackData(DEBUG_logger,
                tracks,
                waypoints.Select(it => (it, WayPointKind.Regular)).Concat(endpoints.Select(it => (it, WayPointKind.Endpoint))),
                offTrackDistance, segmentLengthLimit, onProgress, token);
        }

        internal static IPlanData ProcessTrackData(
            MetaLogger DEBUG_logger,
            IEnumerable<IReadOnlyList<GeoPoint>> tracks,
    IEnumerable<GeoPoint> waypoints,
    Length offTrackDistance, Length segmentLengthLimit, Action<Stage, long, long> onProgress, CancellationToken token)
        {
            return ProcessTrackData(DEBUG_logger, tracks,
                waypoints, Enumerable.Empty<GeoPoint>(),
                offTrackDistance, segmentLengthLimit, onProgress, token);
        }

        internal static bool TryLoadGpx(string filename, out List<List<GeoPoint>> tracks, out List<GeoPoint> waypoints,
            Action<long, long> onProgress, CancellationToken token)
        {
            bool result = tryLoadGpx(filename, out tracks, out List<(GeoPoint point, WayPointKind kind)> waypoints_out,
                onProgress, token);
            waypoints = waypoints_out.Where(it => it.kind == WayPointKind.Regular).Select(it => it.point).ToList();
            return result;
        }
#endif

        internal static IPlanData processTrackData(
#if DEBUG
            MetaLogger DEBUG_Logger,
#endif
            IEnumerable<IReadOnlyList<GeoPoint>> tracks,
            IEnumerable<(GeoPoint point, WayPointKind kind)> waypoints,
            Length offTrackDistance, Length segmentLengthLimit,
            Action<Stage, long, long> onProgress, CancellationToken token)
        {
            Dictionary<GeoPoint, WayPointInfo> waypoints_dict;
            waypoints_dict = new Dictionary<GeoPoint, WayPointInfo>();
            foreach (var entry in (waypoints ?? Enumerable.Empty<(GeoPoint point, WayPointKind kind)>()))
            {
                if (!waypoints_dict.ContainsKey(entry.point))
                    waypoints_dict.TryAdd(entry.point, new WayPointInfo(entry.kind));
            }

            List<Track> tracks_list = tracks.Where(it => it.Count > 1).Select(it => new Track(it)).ToList();

            // add only distant intersections from user's already marked waypoints

            if (!tryFindCrossroads(tracks_list, offTrackDistance, onProgress,
                out List<Crossroad> crossroads_found, token))
                return null;

            if (token.IsCancellationRequested)
                return null;

            cleanupWaypointsAndCrossroads(ref waypoints_dict, crossroads_found, offTrackDistance);

            if (!addEndpoints(tracks_list, waypoints_dict.Keys, crossroads_found, offTrackDistance * 2, onProgress, token))
                return null;

            ITurnGraph turn_graph;
#if !DEBUG
            try
            {
#endif
            turn_graph = createTurnGraph(
#if DEBUG
                DEBUG_Logger,
#endif
                tracks_list, waypoints_dict, crossroads_found,
                offTrackDistance, segmentLengthLimit: segmentLengthLimit, onProgress);
#if !DEBUG
            }
            catch (Exception ex)
            {
                turn_graph = null;
            }
#endif

            GeoPoint[] plan_cxx = waypoints_dict.Keys.Concat(crossroads_found
            // do not export extensions of the tracks, only true intersections or passing by "> * <"
            // which we treat as almost-intersection
                .Where(it => it.Kind != CrossroadKind.Extension)
                .Select(it => it.Point))
                .ToArray();

            IEnumerable<ISegment> segments = tracks_list.SelectMany(trk => trk.Nodes.Where(it => !it.IsLast)).ToArray();

            return new PlanData(
                segments,
                plan_cxx,
                crossroads_found.Count(it => it.Kind == CrossroadKind.Extension),
                // do not pass crossroads as track points because when someone created track manually, 
                // crossroad can be off the track slightly
                turn_graph
                );
        }

        private static bool addEndpoints(IEnumerable<Track> tracks, IEnumerable<GeoPoint> waypoints,
              List<Crossroad> crossroads, Length proximityDistance, Action<Stage, long, long> onProgress, CancellationToken token)
        {
            int total_steps = tracks.Count();
            long step = 0;

            IEnumerable<GeoPoint> initial_crossroads = crossroads.Select(it => it.Point).Concat(waypoints).ToArray();
            var extensions = new List<Crossroad>();

            foreach (Track track in tracks)
            {
                if (token.IsCancellationRequested)
                    return false;

                onProgress?.Invoke(Stage.AddingEndpoints, step, total_steps);
                ++step;

                if (!isPointInProximity(track.Head.Point, initial_crossroads, proximityDistance))
                {
                    extensions.Add(new Crossroad(track.Head.Point, CrossroadKind.Endpoint)
                        .Connected(track.Head, Length.Zero));
                }
                TrackNode last_node = track.Nodes.Last();
                if (!isPointInProximity(last_node.Point, initial_crossroads, proximityDistance))
                {
                    extensions.Add(new Crossroad(last_node.Point, CrossroadKind.Endpoint)
                        .Connected(last_node, Length.Zero));
                }
            }

            var trashed = new HashSet<Crossroad>();
            for (int i = 0; i < extensions.Count; ++i)
                for (int k = i + 1; k < extensions.Count; ++k)
                {
                    if (GeoCalculator.GetDistance(extensions[i].Point, extensions[k].Point) <= proximityDistance)
                    {
                        trashed.Add(extensions[i]);
                        trashed.Add(extensions[k]);
                    }
                }

            extensions.RemoveRange(trashed);

            crossroads.AddRange(extensions);

            return true;
        }

        private static bool isPointInProximity(GeoPoint point, IEnumerable<GeoPoint> crossroads, Length distance)
        {
            foreach (GeoPoint cx in crossroads)
                if (GeoCalculator.GetDistance(cx, point) <= distance)
                    return true;

            return false;
        }

        private static bool tryLoadGpx(string filename, out List<List<GeoPoint>> tracks,
            out List<(GeoPoint point, WayPointKind kind)> waypoints,
            Action<long, long> onProgress,
            CancellationToken token)
        {
            tracks = new List<List<GeoPoint>>();
            waypoints = new List<(GeoPoint point, WayPointKind kind)>();
            using (GpxIOFactory.CreateReader(filename, out IGpxReader reader, out IStreamProgress stream_progress))
            {
                while (reader.Read(out GpxObjectType type))
                {
                    onProgress?.Invoke(stream_progress.Position, stream_progress.Length);

                    if (token.IsCancellationRequested)
                        return false;

                    switch (type)
                    {
                        case GpxObjectType.Metadata:
                            break;
                        case GpxObjectType.WayPoint:
                            {
                                string name = reader.WayPoint.Name ?? "";
                                if (name.StartsWith("-"))
                                {
                                    ; // hack, treat such object only as display/visual hint, do not use it for navigation
                                }
                                else
                                {
                                    GeoPoint pt = GpxHelper.FromGpx(reader.WayPoint);
                                    waypoints.Add((pt, (name.StartsWith("end") || name.StartsWith("exit")) ? WayPointKind.Endpoint : WayPointKind.Regular));
                                }
                                break;
                            }
                        case GpxObjectType.Route:
                            break;
                        case GpxObjectType.Track:
                            {
                                string name = reader.Track.Name ?? "";
                                if (name.StartsWith("-"))
                                {
                                    ; // hack, treat such object only as display/visual hint, do not use it for navigation
                                }
                                else
                                {
                                    if (tracks.Capacity < tracks.Count + reader.Track.Segments.Count)
                                        tracks.Capacity = tracks.Count + reader.Track.Segments.Count;
                                    foreach (var seg in reader.Track.Segments)
                                    {
                                        // allocate list in advance
                                        var trk = new List<GeoPoint>(capacity: seg.TrackPoints.Count);
                                        foreach (var pt in seg.TrackPoints)
                                            trk.Add(GpxHelper.FromGpx(pt));
                                        tracks.Add(trk);
                                    }
                                }
                                break;
                            }
                    }
                }
            }

            return true;
        }

        private static void cleanupWaypointsAndCrossroads(ref Dictionary<GeoPoint, WayPointInfo> userWaypoints,
            List<Crossroad> computedCrossroads, Length limit)
        {
            // here we compute average position for close waypoints and crossroads
            // what's move we move neighbour information from crossroads to waypoints, so even if given waypoint is far from actual track
            // thanks to getting info from crossroad we would know about "connected" tracks
            // it is important for carelessly prepared plans

            // in agressive mode waypoint absorbs all crossroads within the limit, in non-agressive mode it absorbs only the single (closest) crossroads within the limit
            const bool aggressive = false;

            if (aggressive)
            {
                Dictionary<GeoPoint, GeoPoint> mapped_waypoints = userWaypoints.Keys.ToDictionary(it => it, it => it);

                for (int i = computedCrossroads.Count - 1; i >= 0; --i)
                {
                    bool remove = false;

                    foreach (GeoPoint wpt in userWaypoints.Keys)
                    {
                        var cx = computedCrossroads[i];
                        if (GeoCalculator.GetDistance(wpt, cx.Point) <= limit)
                        {
                            remove = true;
                            mapped_waypoints[wpt] = GeoCalculator.GetMidPoint(mapped_waypoints[wpt], cx.Point);
                            userWaypoints[wpt].AddNeighbours(cx.Neighbours.Select(it => it.node));
                        }
                    }

                    if (remove)
                        computedCrossroads.RemoveAt(i);
                }

                {
                    var __waypoints_capture = userWaypoints;
                    userWaypoints = mapped_waypoints.ToDictionary(it => it.Value, it => __waypoints_capture[it.Key]);
                }
            }
            else
            {
                Dictionary<GeoPoint, (int cx_index, Length distance)> mapped_waypoints = userWaypoints.Keys.ToDictionary(it => it, it => (-1, Length.MaxValue));
                var removals = new List<int>();

                for (int i = computedCrossroads.Count - 1; i >= 0; --i)
                {
                    bool remove = false;

                    foreach (GeoPoint wpt in userWaypoints.Keys)
                    {
                        var cx = computedCrossroads[i];
                        if (GeoCalculator.GetDistance(wpt, cx.Point) <= limit)
                        {
                            remove = true;
                            if (mapped_waypoints[wpt].distance > limit)
                                mapped_waypoints[wpt] = (i, limit);
                        }
                    }

                    if (remove)
                        removals.Add(i);
                }

                foreach ((GeoPoint wpt, (int cx_index, Length _)) in mapped_waypoints)
                    if (cx_index != -1)
                        userWaypoints[wpt].AddNeighbours(computedCrossroads[cx_index].Neighbours.Select(it => it.node));

                {
                    var __waypoints_capture = userWaypoints;
                    userWaypoints = mapped_waypoints.ToDictionary(it =>
                        it.Value.cx_index == -1 ? it.Key : GeoCalculator.GetMidPoint(it.Key, computedCrossroads[it.Value.cx_index].Point),
                        it => __waypoints_capture[it.Key]);
                }


                foreach (int i in removals) // IMPORTANT: we iterate from highest to lowest indices here
                    computedCrossroads.RemoveAt(i);
            }
        }

        private static bool tryFindCrossroads(List<Track> tracks,
            Length offTrackDistance, Action<Stage, long, long> onProgress,
            out List<Crossroad> crossroads,
            CancellationToken token)
        {
            crossroads = new List<GpxLoader.Crossroad>();

            {
                IReadOnlyList<Angle> longitude_diff_limits;
                Angle latitude_diff_limit;
                {
                    // note crossroads are computed such way that each track can have crossroad at limit (max) distance
                    // =========<--->*<--->=============
                    // so it means that tracks can be separated by double (!) the limit at most
                    Length proximity_limit = offTrackDistance * 2;

                    // calculating crossroads can be time consuming, so we calculate angular proximity distances
                    // those are just approximations, but they are dirty-cheap -- if two points are not within proximity
                    // it means for sure they won't make crossroad, if they are -- we need to calculate it for real
                    var list = new List<Angle>(capacity: tracks.Count);
                    foreach (var trk in tracks)
                    {
                        // we take max from latitude, because closer the poles the distances are smaller
                        Angle latitude_max = trk.Nodes.Select(it => it.Point.Latitude.Abs()).Max();
                        var longitude_diff = GeoCalculator.GetLongitudeDifference(latitude_max, proximity_limit);
                        list.Add(longitude_diff);
                    }
                    longitude_diff_limits = list;

                    {
                        // distances along longitudes are always the same
                        var dest = GeoCalculator.GetDestination(new GeoPoint(), Angle.Zero, proximity_limit);
                        latitude_diff_limit = dest.Latitude;
                    }
                }

                int total_steps = (tracks.Count - 1) * tracks.Count / 2;
                int step = 0;
                for (int i_idx = 0; i_idx < tracks.Count; ++i_idx)
                    for (int k_idx = i_idx + 1; k_idx < tracks.Count; ++k_idx, ++step)
                    {
                        if (token.IsCancellationRequested)
                            return false;

                        onProgress?.Invoke(Stage.ComputingCrossroads, step, total_steps);

                        // mark each intersection with source index, this will allow us better averaging the points
                        List<Crossroad> intersections = getTrackIntersections(tracks[i_idx], tracks[k_idx],
                            offTrackDistance,
                            latitudeProximityLimit: latitude_diff_limit,
                            longitudeProximityLimit: longitude_diff_limits[i_idx].Max(longitude_diff_limits[k_idx]));
                        intersections.ForEach(it => { it.SetSourceIndex(i_idx, k_idx); });

                        // at this point each track can be enriched with connection into to this or that crossroad
                        crossroads.AddRange(intersections);

                        {
                            IEnumerable<Crossroad> extensions = getTrackExtensions(tracks[i_idx], tracks[k_idx],
                                offTrackDistance);
                            // at this point each track can be enriched with connection into to this or that crossroad
                            crossroads.AddRange(extensions.Select(it => { it.SetSourceIndex(i_idx, k_idx); return it; }));
                        }
                    }
            }

            if (token.IsCancellationRequested)
                return false;

            removePassingBy(crossroads, offTrackDistance * 2, onProgress);

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

            return true;
        }

        /*        public static void WriteGpxPoints(string path, IEnumerable<GeoPoint> points)
                {
                    using (var file = new GpxDirtyWriter(path))
                    {
                        foreach (var p in points)
                            file.WritePoint(null, p);
                    }
                }
                */
        /*public static void WriteGpxSegments(string path, IEnumerable<ISegment> segments)
        {
            using (var file = new GpxDirtyWriter(path))
            {
                int count = 0;
                foreach (ISegment s in segments)
                    file.WriteTrack($"seg{count++}", new[] { s.A, s.B });
            }
        }*/
        private static void removePassingBy(List<GpxLoader.Crossroad> crossroads, Length limit, Action<Stage, long, long> onProgress)
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
                onProgress?.Invoke(Stage.RemovePassingBy, crossroads.Count - i, crossroads.Count);

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
                        Crossroad outcome = null;
                        if (same_tracks)// && a.Kind != GpxLoader.CrossroadKind.Intersection && b.Kind != GpxLoader.CrossroadKind.Intersection)
                        {
                            // having two intersection nearby we check if those are from the same pairs of tracks
                            // if yes, we treat them as extension if just one of them is extension, this way we avoid
                            // spurious results with a lot of intersections when in reality they are caused by imperfections
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

                            // if we have at single point double extension it is rather passing-by kind
                            // (not rock solid science though)
                            if (a.Kind == GpxLoader.CrossroadKind.Extension && b.Kind == GpxLoader.CrossroadKind.Extension)
                                kind = GpxLoader.CrossroadKind.PassingBy;
                            else if (a.Kind == GpxLoader.CrossroadKind.Extension)
                            {
                                outcome = a;
                            }
                            else if (b.Kind == GpxLoader.CrossroadKind.Extension)
                            {
                                outcome = b;
                            }
                            // also we have to deal with passing-by points like in shape "> * <"
                            // this is actually "else" case but we add this condition as sanity check
                            else if (a.Kind == GpxLoader.CrossroadKind.PassingBy && b.Kind == GpxLoader.CrossroadKind.PassingBy)
                                kind = GpxLoader.CrossroadKind.PassingBy;
                            //else
                            //  throw new ($"This case is not possible, right? {a.Kind}, {b.Kind}");
                        }

                        crossroads[i] = outcome ?? GpxLoader.Crossroad.AverageCrossroads(a, b, kind, same_tracks ? a.SourceIndex : null);

                        crossroads[k] = crossroads[crossroads.Count - 1];
                        crossroads.RemoveAt(crossroads.Count - 1);

                        changed = true;
                    }
                }

            return changed;
        }

        internal static bool tryGetSegmentIntersection(in GeoPoint point10, in GeoPoint point11,
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

            if (sig_cx1_len.Abs() <= sig_cx2_len.Abs())
            {
                sig_cx_len1_0 = sig_cx1_len;
                result = cx1;
                return true;
            }
            else //if (sig_cx1_len.Abs() > sig_cx2_len.Abs())
            {
                sig_cx_len1_0 = sig_cx2_len;
                result = GeoCalculator.OppositePoint(cx1);
                return true;
            }
            //else
            //  throw new ($"Cannot decide which intersection to pick.");

        }


        private static bool isWithinLimit(Length signedLength, Length segmentLength, Length limit)
        {
            if (signedLength.Sign() == segmentLength.Sign())
            {
                return signedLength.Abs() <= segmentLength.Abs() + limit;
            }
            else
            {
                return signedLength.Abs() <= limit;
            }
        }

        private static bool computeDistances(in GeoPoint cx,
            in GeoPoint point11,
            in GeoPoint point20, in GeoPoint point21,
            Length sig_len1, ref Length sig_len2, Length limit,
            Length cx_len1_0, ref Length sig_cx_len1_1, ref Length sig_cx_len2_0, ref Length sig_cx_len2_1)
        {
            // those check are basics -- if the interesection is too far away, 
            // it is not really an intersection of the tracks
            if (!isWithinLimit(-cx_len1_0, sig_len1, limit))
                return false;

            // todo: we should be able to compute the distance by simple subtracting
            sig_cx_len1_1 = GeoCalculator.GetSignedDistance(cx, point11);

            sig_len2 = GeoCalculator.GetSignedDistance(point20, point21);

            sig_cx_len2_0 = GeoCalculator.GetSignedDistance(cx, point20);
            if (!isWithinLimit(-sig_cx_len2_0, sig_len2, limit))
                return false;

            sig_cx_len2_1 = GeoCalculator.GetSignedDistance(cx, point21);

            return true;
        }

        private static bool tryGetTrackExtensions(TrackNode track1Node, TrackNode track2Node,
            Length limit, out GpxLoader.Crossroad crossroad)
        {
            // todo: combine mid point and distance in one call
            Length distance = GeoCalculator.GetDistance(track1Node.Point, track2Node.Point);
            if (distance >= limit)
            {
                crossroad = null;
                return false;
            }

            distance /= 2;
            GeoPoint ext_point = GeoCalculator.GetMidPoint(track1Node.Point, track2Node.Point);

            crossroad = new Crossroad(ext_point, CrossroadKind.Extension)
                .Connected(track1Node, distance)
                .Connected(track2Node, distance);

            return true;
        }


        private static IEnumerable<GpxLoader.Crossroad> getTrackExtensions(Track track1, Track track2, Length limit)
        {
            TrackNode head1 = track1.Head;
            TrackNode last1 = track1.Nodes.Last();
            TrackNode head2 = track2.Head;
            TrackNode last2 = track2.Nodes.Last();

            Crossroad cx;
            if (tryGetTrackExtensions(head1, head2, limit, out cx))
                yield return cx;
            if (tryGetTrackExtensions(head1, last2, limit, out cx))
                yield return cx;
            if (tryGetTrackExtensions(last1, head2, limit, out cx))
                yield return cx;
            if (tryGetTrackExtensions(last1, last2, limit, out cx))
                yield return cx;
        }

        private static List<GpxLoader.Crossroad> getTrackIntersections(Track track1, Track track2, Length limit,
            Angle latitudeProximityLimit, Angle longitudeProximityLimit)
        {
            var result = new List<Crossroad>();

            int __idx1 = 0;
            foreach (TrackNode node1_0 in track1.Nodes.Where(it => !it.IsLast))
            {
                ++__idx1;

                TrackNode node1_1 = node1_0.Next;

                Length sig_len1 = Length.Zero;
                Length len1 = Length.Zero;

                int __idx2 = 0;
                foreach (TrackNode node2_0 in track2.Nodes.Where(it => !it.IsLast))
                {
                    ++__idx2;

                    TrackNode node2_1 = node2_0.Next;

                    bool within_proximity = withinProximity(node1_0.Point, node1_1.Point, node2_0.Point, node2_1.Point,
                        latitudeProximityLimit, longitudeProximityLimit);
                    if (!within_proximity)
                        continue;

                    if (len1 == Length.Zero)
                    {
                        sig_len1 = GeoCalculator.GetSignedDistance(node1_0.Point, node1_1.Point);
                        len1 = sig_len1.Abs();
                    }

                    Length sig_len2 = Length.Zero;

                    Length sig_cx_len1_1 = Length.Zero;
                    Length sig_cx_len2_0 = Length.Zero;
                    Length sig_cx_len2_1 = Length.Zero;

                    bool nearby_intersection = tryGetSegmentIntersection(node1_0.Point, node1_1.Point,
                        node2_0.Point, node2_1.Point,
                        out Length sig_cx_len1_0, out GeoPoint cx)
                        && computeDistances(cx, node1_1.Point, node2_0.Point, node2_1.Point,
                        sig_len1, ref sig_len2, limit,
                        sig_cx_len1_0, ref sig_cx_len1_1, ref sig_cx_len2_0, ref sig_cx_len2_1);

                    if (!nearby_intersection)
                    {
                        // finding near intersection failed, but maybe it is caused by two almost parallel segments
                        // in such those two segments could be close to each other, but their intersection could be far away
                        // like in |\
                        // in such case we check distances between the endings of the segments

                        // on success we don't report intersection kind, because we don't have real one
                        bool middle1 = !node1_0.IsFirst && !node1_1.IsLast;
                        bool middle2 = !node2_0.IsFirst && !node2_1.IsLast;

                        if (middle1 || middle2)
                        {
                            Length track_track_dist;
                            Length dist_along_segment;
                            track_track_dist = node1_0.Point
                                .GetDistanceToArcSegment(node2_0.Point, node2_1.Point, out cx, out dist_along_segment);
                            if (track_track_dist < limit)
                            {
                                var mid_cx = GeoCalculator.GetMidPoint(cx, node1_0.Point);
                                var crossroad = new GpxLoader.Crossroad(mid_cx, GpxLoader.CrossroadKind.PassingBy)
                                    .Connected(node1_0, track_track_dist / 2)
                                    .Projected(node2_0, cx, dist_along_segment);

                                result.Add(crossroad);
                            }
                            else
                            {
                                track_track_dist = node1_1.Point
                                    .GetDistanceToArcSegment(node2_0.Point, node2_1.Point, out cx, out dist_along_segment);
                                if (track_track_dist < limit)
                                {
                                    var mid_cx = GeoCalculator.GetMidPoint(cx, node1_1.Point);
                                    GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(mid_cx, GpxLoader.CrossroadKind.PassingBy)
                                        .Connected(node1_1, track_track_dist / 2)
                                        .Projected(node2_0, cx, dist_along_segment);

                                    result.Add(crossroad);
                                }
                                else
                                {
                                    track_track_dist = node2_0.Point
                                        .GetDistanceToArcSegment(node1_0.Point, node1_1.Point, out cx, out dist_along_segment);
                                    if (track_track_dist < limit)
                                    {
                                        var mid_cx = GeoCalculator.GetMidPoint(cx, node2_0.Point);
                                        GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(mid_cx, GpxLoader.CrossroadKind.PassingBy)
                                            .Connected(node2_0, track_track_dist / 2)
                                            .Projected(node1_0, cx, dist_along_segment);

                                        result.Add(crossroad);
                                    }
                                    else
                                    {
                                        track_track_dist = node2_1.Point
                                            .GetDistanceToArcSegment(node1_0.Point, node1_1.Point, out cx, out dist_along_segment);
                                        if (track_track_dist < limit)
                                        {
                                            var mid_cx = GeoCalculator.GetMidPoint(cx, node2_1.Point);
                                            GpxLoader.Crossroad crossroad = new GpxLoader.Crossroad(mid_cx, GpxLoader.CrossroadKind.PassingBy)
                                                .Connected(node2_1, track_track_dist / 2)
                                                .Projected(node1_0, cx, dist_along_segment);

                                            result.Add(crossroad);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else // we have actual intersection (at least within limits)
                    {
                        Length cx_len1_0 = sig_cx_len1_0.Abs();
                        Length cx_len1_1 = sig_cx_len1_1.Abs();
                        Length cx_len2_0 = sig_cx_len2_0.Abs();
                        Length cx_len2_1 = sig_cx_len2_1.Abs();

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

                        // if we are in the middle of track we can get such shape "> * <" or "- *|"
                        // the overall distance cannot be too far...
                        // we divide by "2" because when we are in the middle of the segment the difference will be zero
                        // but when we are outside without division we would count extension twice (from each of the points)
                        Length len1_diff = (cx_len1_0 + cx_len1_1 - len1) / 2;
                        Length len2_diff = (cx_len2_0 + cx_len2_1 - len2) / 2;

                        bool pick1_1 = len1 < cx_len1_0;
                        bool pick2_1 = len2 < cx_len2_0;

                        if (in_middle_track1 || in_middle_track2)
                        {
                            Length diff_total = len1_diff + len2_diff;
                            if (diff_total > limit)
                                continue;
                            // and we have to detect passing-by "> * <", if in both cases crossing point is outside
                            // segment we have passing-by point
                            else if (len1_diff > numericAccuracy && len2_diff > numericAccuracy)
                            {
                                Crossroad crossroad = new Crossroad(cx, GpxLoader.CrossroadKind.PassingBy);

                                crossroad.Connected(pick1_1 ? node1_1 : node1_0, pick1_1 ? cx_len1_1 : cx_len1_0);
                                crossroad.Connected(pick2_1 ? node2_1 : node2_0, pick2_1 ? cx_len2_1 : cx_len2_0);

                                result.Add(crossroad);
                            }
                            else
                            {
                                Crossroad crossroad = new GpxLoader.Crossroad(cx, CrossroadKind.Intersection);
                                // we can project only ONTO given segment, we cannot project out of it because
                                // it would lengthen it, instead of splitting it
                                if (len1_diff > Length.Zero) // outside segment
                                    crossroad.Connected(pick1_1 ? node1_1 : node1_0, pick1_1 ? cx_len1_1 : cx_len1_0);
                                else
                                    crossroad.Projected(node1_0, cx, cx_len1_0);
                                if (len2_diff > Length.Zero) // outside segment
                                    crossroad.Connected(pick2_1 ? node2_1 : node2_0, pick2_1 ? cx_len2_1 : cx_len2_0);
                                else
                                    crossroad.Projected(node2_0, cx, cx_len2_0);

                                result.Add(crossroad);
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
                            /*crossroad = new Crossroad(cx, GpxLoader.CrossroadKind.Extension);
                            {
                                bool first = cx_len1_0 > len1;
                                crossroad.Connected(first ? node1_1 : node1_0, first ? cx_len1_1 : cx_len1_0);
                            }
                            {
                                bool first = cx_len2_0 > len2;
                                crossroad.Connected(first ? node2_1 : node2_0, first ? cx_len2_1 : cx_len2_0);
                            }*/
                        }
                        else
                        {
                            Crossroad crossroad = new GpxLoader.Crossroad(cx, CrossroadKind.Intersection);
                            if (len1_diff > Length.Zero) // outside segment
                                crossroad.Connected(pick1_1 ? node1_1 : node1_0, pick1_1 ? cx_len1_1 : cx_len1_0);
                            else
                                crossroad.Projected(node1_0, cx, cx_len1_0);
                            if (len2_diff > Length.Zero) // outside segment
                                crossroad.Connected(pick2_1 ? node2_1 : node2_0, pick2_1 ? cx_len2_1 : cx_len2_0);
                            else
                                crossroad.Projected(node2_0, cx, cx_len2_0);

                            result.Add(crossroad);
                        }
                    }
                }
            }

            return result;
        }

        private static bool withinProximity(GeoPoint point1_0, GeoPoint point1_1, GeoPoint point2_0, GeoPoint point2_1,
            Angle latitudeProximityLimit, Angle longitudeProximityLimit)
        {
            {
                Angle diff = (point1_0.Latitude - point2_0.Latitude).Abs() - latitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }
            {
                Angle diff = (point1_1.Latitude - point2_0.Latitude).Abs() - latitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }
            {
                Angle diff = (point1_0.Latitude - point2_1.Latitude).Abs() - latitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }
            {
                Angle diff = (point1_1.Latitude - point2_1.Latitude).Abs() - latitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }

            {
                Angle diff = (point1_0.Longitude - point2_0.Longitude).Abs() - longitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }
            {
                Angle diff = (point1_1.Longitude - point2_0.Longitude).Abs() - longitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }
            {
                Angle diff = (point1_0.Longitude - point2_1.Longitude).Abs() - longitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }
            {
                Angle diff = (point1_1.Longitude - point2_1.Longitude).Abs() - longitudeProximityLimit;
                if (diff <= Angle.Zero)
                    return true;
            }

            return false;
        }
    }
}
