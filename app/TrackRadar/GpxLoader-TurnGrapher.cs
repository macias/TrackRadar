using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public sealed partial class GpxLoader
    {
        internal static ITurnGraph createTurnGraph(IEnumerable<Track> tracks,
            IReadOnlyDictionary<GeoPoint, WayPointKind> waypoints, IEnumerable<Crossroad> crossroads,
            Length offTrackDistance, Length segmentLengthLimit, Action<Stage, double> onProgress)
        {
            // if there are no turns at all, simply disable navigation (one factor which is important as well here is
            // the user will be notified the plan does not have indicated or computed turns at all)
            if (crossroads.Count(it => it.Kind != CrossroadKind.Extension) == 0 && waypoints.Count() == 0)
                return null;

            splitTracksByCrossroad(crossroads);
            if (segmentLengthLimit > Length.Zero)
                splitTracksByLength(tracks, segmentLengthLimit);

            var priority_queue = new NodeQueue();

            {
                // get track nodes closest to waypoints

                splitTracksByWaypoints(tracks, waypoints.Keys, offTrackDistance, priority_queue, onProgress);

                // as above -- this time get closest track nodes to crossroads (i.e. computed on the fly)
                foreach (Crossroad cx in crossroads.Where(it => it.Kind != CrossroadKind.Extension))
                {
                    foreach ((TrackNode neighbour, Length? dist) in cx.Neighbours)
                    {
                        priority_queue.Update(neighbour, turnPoint: cx.Point, hops: 0, dist.Value);
                    }
                }
            }

            // if two adjacent track nodes are assigned to two different turn points add one track node between them
            // without it we couldn't add info about alternate turn point to the segment because there would be no node
            // to attach to (alternates are added only to non-turn nodes)
            foreach ((TrackNode node, GeoPoint turn_point) in priority_queue.NodeTurnPoints)
            {
                if (node.Next != null
                    && priority_queue.TryGetTurnPoint(node.Next, out GeoPoint other_turn)
                    && turn_point != other_turn)
                {
                    node.Add(GeoCalculator.GetMidPoint(node.Point, node.Next.Point));
                }
            }

            IReadOnlyDictionary<TrackNode, (TrackNode ext_node, Length distance)> rich_extensions
                = buildTrackExtensions(crossroads, turnNodes: null);

            // at this point we have priority queue with all turn points and their closest track nodes
            // since we know the adjacent nodes to any given track node we can compute what is the closest turn
            // point to any given track point using Dijkstra approach
            IReadOnlyDictionary<GeoPoint, IEnumerable<(TrackNode adjNode, Length distance)>> track_point_connections
                = aggregateNodeConnections(tracks, rich_extensions);
            // track point -> closest turn point
            var track_to_turns = new Dictionary<GeoPoint, TurnPointInfo>();

            var turns_to_tracks = new Dictionary<GeoPoint, List<(TrackNode turn, TrackNode.Direction dir)>>();

            var turn_nodes = new HashSet<GeoPoint>();

            double total_steps = tracks.Sum(it => it.Nodes.Count());
            double current_step = -1;

            {
                var endpoints = waypoints.Where(it => it.Value == WayPointKind.Endpoint).Select(it => it.Key)
                    .Concat(crossroads.Where(it => it.Kind == CrossroadKind.Endpoint).Select(it => it.Point))
                    .ToHashSet();

                while (priority_queue.TryPop(out Length current_dist, out TrackNode track_node, out GeoPoint turn_point, out int hops))
                {
                    onProgress?.Invoke(Stage.AssigningTurns, ++current_step / total_steps);

                    if (hops == 0)
                    {
                        turn_nodes.Add(track_node.Point);

                        // for endpoints we don't provide turn navigation info (go right/left), so there is no sense in computing it
                        if (!endpoints.Contains(turn_point))
                        {
                            if (!turns_to_tracks.TryGetValue(turn_point, out List<(TrackNode turn, TrackNode.Direction dir)> turn_node_arms))
                            {
                                turn_node_arms = new List<(TrackNode turn, TrackNode.Direction dir)>();
                                turns_to_tracks.Add(turn_point, turn_node_arms);
                            }
                            if (track_node.Next != null)
                                turn_node_arms.Add((track_node, TrackNode.Direction.Forward));
                            if (!track_node.IsFirst)
                                turn_node_arms.Add((track_node, TrackNode.Direction.Backward));
                        }
                    }

                    // this particular point might be already addded, but thanks to aggregated siblings
                    // we don't need to process it in such case
                    if (track_to_turns.TryAdd(track_node.Point, new TurnPointInfo(turn_point, current_dist)))
                    {
                        foreach ((TrackNode sibling, Length sib_distance) in track_point_connections[track_node.Point])
                        {
                            Length total = current_dist + sib_distance;

                            priority_queue.Update(sibling, turnPoint: turn_point, hops + 1, total);
                        }
                    }
                }
            }

            assignSectionsIds(tracks, turn_nodes, buildTrackExtensions(crossroads, turn_nodes), onProgress);

            IReadOnlyDictionary<GeoPoint, TurnPointInfo> alt_track_to_turns = calculateAlternateTurns(tracks,
                track_to_turns, turn_nodes, rich_extensions, onProgress);

            // turn POINTS (not nodes) to adjacent turn POINTS
            IReadOnlyDictionary<GeoPoint, IEnumerable<TurnPointInfo>> turn_points_graph =
                calculateTurnsGraph(tracks, track_point_connections, track_to_turns, alt_track_to_turns, turn_nodes, onProgress);

            var turns_to_arms = new Dictionary<GeoPoint, (TurnArm a, TurnArm b)>();
            foreach (KeyValuePair<GeoPoint, List<(TrackNode turn, TrackNode.Direction dir)>> entry in turns_to_tracks)
            {
                // we can only tell turn direction when the turn has 2 arms (incoming and outgoing)
                if (entry.Value.Count == 2)
                {
                    TurnArm arm_a = buildArmSection(entry.Key, entry.Value[0].turn, entry.Value[0].dir, track_to_turns);
                    TurnArm arm_b = buildArmSection(entry.Key, entry.Value[1].turn, entry.Value[1].dir, track_to_turns);
#if DEBUG
                    if (arm_a.SectionId == arm_b.SectionId)
                        throw new InvalidOperationException($"Both arms cannot have the same section id {arm_a.SectionId}");
#endif
                    turns_to_arms.Add(entry.Key, (arm_a, arm_b));
                }
            }

            return new TurnGraph(turn_nodes, track_to_turns, alt_track_to_turns, turns_to_arms, turn_points_graph);
        }

        private static IReadOnlyDictionary<GeoPoint, IEnumerable<TurnPointInfo>>
            calculateTurnsGraph(IEnumerable<Track> tracks,
            IReadOnlyDictionary<GeoPoint, IEnumerable<(TrackNode adjNode, Length distance)>> trackPointConnections,
            Dictionary<GeoPoint, TurnPointInfo> trackToTurns, IReadOnlyDictionary<GeoPoint, TurnPointInfo> altTrackToTurns,
            HashSet<GeoPoint> turnNodes,
            Action<Stage, double> onProgress)
        {
            var result = new Dictionary<GeoPoint, IEnumerable<TurnPointInfo>>();

            //double total_steps = tracks.Count();
            //double current_step = -1;

            // such peculiar iteration/grouping is needed, because two different nodes can hit the same
            // turning point and each of them can bring different data, consider
            //  \     /
            //   > * < 
            //  /     \
            // left track "corner" and right track "corner" hit the same turning point (*)
            // and they have different adjacent nodes
            foreach (IGrouping<GeoPoint, (TrackNode node, GeoPoint turnPoint)> node_group in tracks
                .SelectMany(it => it.Nodes)
                .Select(it =>
                {
                    if (turnNodes.Contains(it.Point))
                        return ((node: it, turnPoint: trackToTurns[it.Point].TurnPoint));
                    else
                        return (null, default);
                })
                .Where(it => it.node != null) // bring only turning nodes
                .GroupBy(it => it.turnPoint))
            {
                //onProgress?.Invoke(Stage.TurnsToTurnsGraph, ++current_step / total_steps);

                var adj_turn_points = new Dictionary<GeoPoint, Length>();

                foreach (TrackNode turn_node in node_group.Select(it => it.node))
                {
                    foreach ((TrackNode adj_node, Length dist) in trackPointConnections[turn_node.Point])
                    {
                        var primary_info = trackToTurns[adj_node.Point];
                        if (primary_info.TurnPoint != node_group.Key)
                            adj_turn_points.TryAdd(primary_info.TurnPoint, primary_info.Distance + dist);

                        if (altTrackToTurns.TryGetValue(adj_node.Point, out TurnPointInfo secondary_info)
                            && secondary_info.TurnPoint != node_group.Key)
                            adj_turn_points.TryAdd(secondary_info.TurnPoint, secondary_info.Distance + dist);
                    }

                }

                // we use dictionary to avoid duplicates
                result.Add(node_group.Key, adj_turn_points.Select(it => new TurnPointInfo(it.Key, it.Value)).ToList());
            }

            return result;

        }

        private static TrackNode go(TrackNode node, IReadOnlyDictionary<TrackNode, (TrackNode ext_node, Length distance)> extensions, ref TrackNode.Direction dir)
        {
            TrackNode following = node.Go(dir);
            if (following == null && extensions.TryGetValue(node, out var other_end))
            {
                dir = other_end.ext_node.IsFirst ? TrackNode.Direction.Forward : TrackNode.Direction.Backward;
                return other_end.ext_node;
            }
            else
                return following;
        }

        private static Dictionary<GeoPoint, TurnPointInfo> calculateAlternateTurns(IEnumerable<Track> tracks,
            Dictionary<GeoPoint, TurnPointInfo> trackToTurns,
            HashSet<GeoPoint> turnNodes, IReadOnlyDictionary<TrackNode, (TrackNode ext_node, Length distance)> richExtensions,
            Action<Stage, double> onProgress)
        {
            // (a)-b--(c)
            // from "b" the closest turn node is "a", the question is what is the second adjacent turn to "b"
            // this info answers the question what is the next turn when we are moving AWAY from the closest turn

            var alt_track_to_turns = new Dictionary<GeoPoint, TurnPointInfo>();

            void set_alternate_turn(TrackNode node, TrackNode.Direction dir)
            {
                // this computes alternate only in one direction (so it is single-pass algorithm)
                // to compute the other part of the section run it from the other end in reverse direction
                TurnPointInfo left_turn_info = trackToTurns[node.Point];
                Length left_distance = left_turn_info.Distance;

                bool switched = false;
                TurnPointInfo last_turn_info = left_turn_info;

                for (TurnPointInfo turn_info; ; last_turn_info = turn_info)
                {
                    TrackNode last_node = node;
                    node = go(node, richExtensions, ref dir);

                    if (node == null || turnNodes.Contains(node.Point))
                        break;

                    turn_info = trackToTurns[node.Point];
                    if (turn_info.TurnPoint == left_turn_info.TurnPoint)
                    {
                        left_distance = turn_info.Distance;
                    }
                    else
                    {
                        if (switched)
                            left_distance += (last_turn_info.Distance - turn_info.Distance);
                        else
                        {
                            // those nodes can come from different tracks/segments
                            left_distance += GeoCalculator.GetDistance(last_node.Point, node.Point);
                            switched = true;
                        }

                        // it is possible that given entry already exist (consider two extension tracks which shares common
                        // geo point). 
                        alt_track_to_turns.TryAdd(node.Point, new TurnPointInfo(left_turn_info.TurnPoint, left_distance));
                    }
                }
            }

            double total_steps = tracks.Count();
            double current_step = -1;

            foreach (Track trk in tracks)
            {
                onProgress?.Invoke(Stage.AlternateTurns, ++current_step / total_steps);

                foreach (TrackNode turn_node in trk.Nodes.Where(it => turnNodes.Contains(it.Point)))
                {
                    set_alternate_turn(turn_node, TrackNode.Direction.Backward);
                    set_alternate_turn(turn_node, TrackNode.Direction.Forward);
                }
            }

            return alt_track_to_turns;
        }

        private static void assignSectionsIds(IEnumerable<Track> tracks,
            HashSet<GeoPoint> turnNodes, IReadOnlyDictionary<TrackNode, (TrackNode ext_node, Length distance)> extensions,
            Action<Stage, double> onProgress)
        {
            int flood_section_id(ref TrackNode node, ref TrackNode.Direction dir, int sectionId)
            {
                bool change = false;
                while (node != null)
                {
                    node = go(node, extensions, ref dir);

                    if (node == null || node.IsSectionSet)
                    {
                        node = null;
                        break;
                    }

                    bool is_turn_node = turnNodes.Contains(node.Point);
                    if (is_turn_node && dir == TrackNode.Direction.Forward && !node.IsLast)
                    {
                        // in forward movement we need new id for turn node except for the last node in the track
                        node.SetSectionId(++sectionId);
                        return sectionId;
                    }

                    node.SetSectionId(sectionId);
                    change = true;

                    if (is_turn_node)
                        break;
                }

                return sectionId + (change ? 1 : 0);
            }

            double total_steps = tracks.Count() * 2;
            double current_step = -1;

            int section_id = -1;

            // note that "starting" node defines the segment, thus with nodes like
            // a-b-c-(d)-e-f
            // when d is turning node, c is the last node of the left part, and d is the first of the right
            // meaning turn node cannot have the same section id as previous node -- the only exception to this is
            // when it is the last node in the track (then it must have it the same)
            foreach (Track trk in tracks)
            {
                onProgress?.Invoke(Stage.SectionId, ++current_step / total_steps);

                if (trk.Head.IsSectionSet)
                    continue;

                // start with the last turn node to assign section id of the last node
                // otherwise we couldn't be sure the second last and last will have the same id
                // (it is a must for producing sane segments)
                TrackNode t_node = trk.Nodes.LastOrDefault(it => turnNodes.Contains(it.Point));
                if (t_node == null)
                    continue;

                ++section_id; // even without turns, each track makes its own section
                              // one of the arms has to get this id
                t_node.SetSectionId(section_id);
                // do not blindly start with forward movement, if it is the last node we start with
                // go backward first to ensure second last and last nodes will get same id
                TrackNode.Direction init_dir = t_node.IsLast ? TrackNode.Direction.Backward : TrackNode.Direction.Forward;
                for ((TrackNode n, TrackNode.Direction dir) = (t_node, init_dir); n != null;)
                {
                    section_id = flood_section_id(ref n, ref dir, section_id);
                    // we cannot unconditionally increase id, because if this part was dead end
                    // the backward stage has to get same id as the turn node
                }
                init_dir = init_dir == TrackNode.Direction.Forward ? TrackNode.Direction.Backward : TrackNode.Direction.Forward;
                for ((TrackNode n, TrackNode.Direction dir) = (t_node, init_dir); n != null;)
                {
                    section_id = flood_section_id(ref n, ref dir, section_id);
                }
            }
            // assign section ids in the "free" tracks (i.e. without turning nodes)
            foreach (Track trk in tracks)
            {
                onProgress?.Invoke(Stage.SectionId, ++current_step / total_steps);

                if (trk.Head.IsSectionSet)
                    continue;

                ++section_id; // each track makes its own section
                trk.Head.SetSectionId(section_id);
                {
                    TrackNode node = trk.Head;
                    TrackNode.Direction dir = TrackNode.Direction.Forward;
                    flood_section_id(ref node, ref dir, section_id);
                }
                {
                    TrackNode node = trk.Head;
                    TrackNode.Direction dir = TrackNode.Direction.Backward;
                    flood_section_id(ref node, ref dir, section_id);
                }
            }

#if DEBUG
            // section ids validation
            foreach (Track trk in tracks)
            {
                if (trk.Nodes.Any(it => !it.IsSectionSet))
                    throw new InvalidOperationException("Section id not set");

                TrackNode second_to_last = trk.Nodes.First(it => it.Next.IsLast);
                if (second_to_last.SectionId != second_to_last.Next.SectionId)
                    throw new InvalidOperationException("Section id integrity problem");

                foreach (TrackNode node in trk.Nodes.Where(it => !it.IsLast && turnNodes.Contains(it.Point)))
                {
                    //if (node.SectionId != node.Next.SectionId)
                    //  throw new InvalidOperationException("Forward turn arm has to have the same id as the turn node itself");

                    if (!node.IsFirst && node.SectionId == node.Go(TrackNode.Direction.Backward).SectionId)
                        // except the last node, but we have condition in the loop for it
                        throw new InvalidOperationException("Backward turn arm cannot have the same id as the turn node itself");
                }
            }
#endif
        }

        private static TurnArm buildArmSection(GeoPoint turnPoint, TrackNode node, TrackNode.Direction dir,
            IReadOnlyDictionary<GeoPoint, TurnPointInfo> track_to_turns)
        {
            var section_id = dir == TrackNode.Direction.Forward ? node.SectionId : node.Go(dir).SectionId;
            var turn_section = new List<(GeoPoint trackPoint, Length distance)>();
            // in order to create segments out of points we need to add one ending point not belonging to this turn
            // in simplest case consider two turns one after other (a) (b)
            // taking only belonging point it means taking only (a), i.e. single point which tells us nothing
            // about segment bearing 
            while (node != null)
            {
                turn_section.Add((node.Point, track_to_turns[node.Point].Distance));
                if (track_to_turns[node.Point].TurnPoint != turnPoint)
                    break;
                node = node.Go(dir);
            }

            return new TurnArm(section_id, turn_section);
        }

        private static void splitTracksByWaypoints(IEnumerable<Track> tracks, IEnumerable<GeoPoint> waypoints,
            Length offTrackDistance, NodeQueue priorityQueue, Action<Stage, double> onProgress)
        {
            double total_steps = waypoints.Count();
            double current_step = -1;
            foreach (GeoPoint wpt in waypoints)
            {
                onProgress?.Invoke(Stage.SplitByWaypoints, ++current_step / total_steps);

                foreach (Track trk in tracks)
                {
                    Length min_distance = Length.MaxValue;
                    TrackNode closest_track_node = default;
                    GeoPoint crosspoint = default;

                    foreach (GpxLoader.TrackNode node in trk.Nodes)
                    {
                        if (node.Next == null)
                            break;

                        Length d = GeoCalculator.GetDistanceToArcSegment(wpt, node.Point, node.Next.Point, out GeoPoint cx);
                        if (min_distance > d)
                        {
                            min_distance = d;
                            closest_track_node = node;
                            crosspoint = cx;
                            // todo: add some numeric slack?
                            if (min_distance == Length.Zero)
                                break;
                        }
                    }
                    if (min_distance < offTrackDistance / 2)
                    {
                        // if reality we are closer to the end of the segment, update what track node we have in mind
                        if (Geo.Mather.SufficientlySame(crosspoint, closest_track_node.Next.Point))
                            closest_track_node = closest_track_node.Next;
                        else if (!Geo.Mather.SufficientlySame(crosspoint, closest_track_node.Point))
                        {
                            closest_track_node = closest_track_node.Add(crosspoint);
                        }


                        priorityQueue.Update(closest_track_node, turnPoint: wpt, hops: 0, min_distance);
                    }
                }
            }
        }

        private static void splitTracksByLength(IEnumerable<Track> tracks, Length segmentLengthLimit)
        {
            foreach (Track trk in tracks)
            {
                TrackNode curr = trk.Head;
                while (curr.Next != null)
                {
                    TrackNode next = curr.Next;
                    splitSegment(curr, curr.GetLength(), segmentLengthLimit);
                    curr = next;
                }
            }
        }

        private static void splitSegment(TrackNode node, Length distance, Length segmentLengthLimit)
        {
            // use separate recursive function to avoid recomputing distance between segment points

            if (distance <= segmentLengthLimit)
                return;

            // todo: pass distance
            var next = node.Add(GeoCalculator.GetMidPoint(node.Point, node.Next.Point));

            Length half_dist = distance / 2;

            splitSegment(node, half_dist, segmentLengthLimit);
            splitSegment(next, half_dist, segmentLengthLimit);
        }

        private static IReadOnlyDictionary<GeoPoint, IEnumerable<(TrackNode sibling, Length distance)>>
            aggregateNodeConnections(IEnumerable<Track> tracks,
            IReadOnlyDictionary<TrackNode, (TrackNode ext_node, Length distance)> extensions)
        {
            var dict = new Dictionary<GeoPoint, IEnumerable<(TrackNode sibling, Length distance)>>();

            foreach (IGrouping<GeoPoint, TrackNode> node_group in tracks
                .SelectMany(it => it.Nodes)
                .GroupBy(it => it.Point))
            {
                var list = new List<(TrackNode sibling, Length distance)>();
                foreach (TrackNode node in node_group)
                {
                    list.AddRange(node.MeasuredSiblings);

                    if (extensions.TryGetValue(node, out var other_end))
                        list.Add((other_end.ext_node, other_end.distance));
                }
                dict.Add(node_group.Key, list);
            }

            return dict;
        }

        private static IReadOnlyDictionary<TrackNode, (TrackNode ext_node, Length distance)> buildTrackExtensions(IEnumerable<Crossroad> crossroads,
            // passing this as non-null indicated we would like to exclude edge turn nodes
            HashSet<GeoPoint> turnNodes)
        {
            var extensions = new Dictionary<TrackNode, (TrackNode ext_node, Length distance)>();
            foreach (var ext_cx in crossroads.Where(it => it.Kind == CrossroadKind.Extension))
            {
                if (!ext_cx.TryGetExtensionPair(out (TrackNode node, Length distance) left,
                    out (TrackNode node, Length distance) right))
                    continue;

                // skip the edge turn nodes, this will simplify assigning section ids
                if (turnNodes != null && (turnNodes.Contains(left.node.Point) || turnNodes.Contains(right.node.Point)))
                    continue;

                Length total_distance = right.distance + left.distance;
                {
                    bool added = extensions.TryAdd(left.node, (right.node, total_distance));
#if DEBUG
                    if (!added)
                        throw new InvalidOperationException($"Node {left.node.Point} from {ext_cx.Point} already added");
#endif
                }
                {
                    bool added = extensions.TryAdd(right.node, (left.node, total_distance));
#if DEBUG
                    if (!added)
                        throw new InvalidOperationException($"Node {right.node.Point} from {ext_cx.Point} already added");
#endif
                }
            }

            return extensions;
        }

        private static void splitTracksByCrossroad(IEnumerable<Crossroad> crossroads)
        {
            var tracks_to_crossroads = new Dictionary<TrackNode, List<(Length dist, Crossroad crossroad, GeoPoint projPoint)>>();

            // first we compute all the projections that go per each segment of the track
            foreach (Crossroad cx in crossroads)
            {
                cx.UpdateProjections();

                foreach ((TrackNode proj_node, GeoPoint proj_point, Length node_proj_distance) in cx.Projections)
                {
                    if (!tracks_to_crossroads.TryGetValue(proj_node, out List<(Length, Crossroad, GeoPoint)> cx_list))
                    {
                        cx_list = new List<(Length, Crossroad, GeoPoint)>();
                        tracks_to_crossroads.Add(proj_node, cx_list);
                    }
                    cx_list.Add((node_proj_distance, cx, proj_point));
                }
            }

            // now we know into how many pieces each segment should be split
            foreach (var entry in tracks_to_crossroads)
            {
                TrackNode current_node = entry.Key;

                foreach ((_, Crossroad cx, GeoPoint proj_point) in entry.Value.OrderBy(it => it.dist))
                {
#if DEBUG
                    if (current_node.IsLast)
                        throw new InvalidOperationException("Cannot add projection to the last node");

                    {
                        Angle curr_bearing = GeoCalculator.GetBearing(current_node.Point, current_node.Next.Point);
                        Angle mid_in_bearing = GeoCalculator.GetBearing(current_node.Point, proj_point);
                        Angle mid_out_bearing = GeoCalculator.GetBearing(proj_point, current_node.Next.Point);

                        Length curr_distance = GeoCalculator.GetDistance(current_node.Point, current_node.Next.Point);
                        Length future_distance = GeoCalculator.GetDistance(current_node.Point, proj_point)
                            + GeoCalculator.GetDistance(proj_point, current_node.Next.Point);
                        const int distance_decimals = 5;
                        const MidpointRounding mode = MidpointRounding.AwayFromZero;
                        if (Math.Round(curr_distance.Meters, distance_decimals, mode) < Math.Round(future_distance.Meters, distance_decimals, mode))
                            throw new InvalidOperationException($"Adding projection increased distance from {curr_distance} to {future_distance}, bearings from {curr_bearing} to {mid_in_bearing}, {mid_out_bearing} at # {current_node.__DEBUG_id}");

                        /*
                        Angle bearing_diff_to = GeoCalculator.AbsoluteBearingDifference(curr_bearing, to_mid_bearing);
                        Angle bearing_diff_from = GeoCalculator.AbsoluteBearingDifference(curr_bearing, from_mid_bearing);
                        const double bearing_limit = 0.005;
                        if (diff_to.Degrees> bearing_limit
                            || diff_from.Degrees > bearing_limit)
                            throw new InvalidOperationException($"Adding projection changed bearing: {curr_bearing}, {to_mid_bearing}, {from_mid_bearing}; differences {diff_to}, {diff_from} at # {current_node.__DEBUG_id}");
                            */
                    }

#endif
                    current_node = current_node.Add(proj_point);
                    // todo: extra computation (later)
                    cx.Connected(current_node, null);
                }
            }

            foreach (Crossroad cx in crossroads)
            {
                cx.RemoveProjections();
                cx.UpdateNeighbours();
            }
        }
    }
}
