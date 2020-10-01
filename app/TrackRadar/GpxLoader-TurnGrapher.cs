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
            IEnumerable<GeoPoint> waypoints, IEnumerable<Crossroad> crossroads,
            Length offTrackDistance, Length segmentLengthLimit)
        {
            splitTracksByCrossroad(crossroads);
            splitTracksByLength(tracks, segmentLengthLimit);

            var priority_queue = new NodeQueue();

            {
                // get track nodes closest to waypoints

                splitTracksByWaypoints(tracks, waypoints, offTrackDistance, priority_queue);

                // as above -- this time get closest track nodes to crossroads (i.e. computed on the fly)
                foreach (Crossroad cx in crossroads)
                {
                    foreach ((TrackNode neighbour, Length? dist) in cx.Neighbours)
                    {
                        priority_queue.Update(neighbour, turnPoint: cx.Point, hops: 0, dist.Value);
                    }
                }
            }

            // at this point we have priority queue with all turn points and their closest track nodes
            // since we know the adjacent nodes to any given track node we can compute what is the closest turn
            // point to any given track point using Dijkstra approach
            IReadOnlyDictionary<GeoPoint, List<(TrackNode sibling, Length distance)>> track_point_siblings 
                = aggregateNodeSiblings(tracks);
            // track point -> closest turn point
            var track_to_turns = new Dictionary<GeoPoint, TurnPointInfo>();

            var turns_to_tracks = new Dictionary<GeoPoint, List<(TrackNode turn, TrackNode.Direction dir)>>();

            var turn_nodes = new HashSet<GeoPoint>();

            while (priority_queue.TryPop(out Length current_dist, out TrackNode track_node, out GeoPoint turn_point, out int hops))
            {
                if (hops == 0)
                {
                    turn_nodes.Add(track_node.Point);

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

                // this particular point might be already addded, but thanks to aggregated siblings
                // we don't need to process it in such case
                if (track_to_turns.TryAdd(track_node.Point, new TurnPointInfo(turn_point, current_dist)))
                {
                    foreach ((TrackNode sibling, Length sib_distance) in track_point_siblings[track_node.Point])
                    {
                        Length total = current_dist + sib_distance;

                        priority_queue.Update(sibling, turnPoint: turn_point, hops + 1, total);
                    }
                }
            }

            {
                int section_id = -1;
                foreach (Track trk in tracks)
                {
                    bool id_used = false;
                    ++section_id; // even without turns, each track makes its own section
                    foreach (TrackNode node in trk.Nodes)
                    {
                        // it means this node is a turn-node (next to -- or at -- actual turn point)
                        // thus sections starts/ends at this node
                        if (turn_nodes.Contains(node.Point) && id_used)
                            ++section_id;
                        node.SetSectionId(section_id);
                        id_used = true;
                    }
                }
            }

            var turns_to_arms = new Dictionary<GeoPoint, (TurnArm a, TurnArm b)>();
            foreach (KeyValuePair<GeoPoint, List<(TrackNode turn, TrackNode.Direction dir)>> entry in turns_to_tracks)
            {
                // we can only tell turn direction when the turn has 2 arms (incoming and outgoing)
                if (entry.Value.Count == 2)
                {
                    turns_to_arms.Add(entry.Key, (buildArmSection(entry.Key, entry.Value[0].turn, entry.Value[0].dir, track_to_turns),
                        buildArmSection(entry.Key, entry.Value[1].turn, entry.Value[1].dir, track_to_turns)));
                }
            }

            return new TurnGraph(track_to_turns, turns_to_arms);
        }

        private static TurnArm buildArmSection(GeoPoint turnPoint, TrackNode node, TrackNode.Direction dir,
            IReadOnlyDictionary<GeoPoint, TurnPointInfo> track_to_turns)
        {
            var section_id = node.SectionId;
            var turn_section = new List<(GeoPoint trackPoint, Length distance)>();
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
            Length offTrackDistance, NodeQueue priorityQueue)
        {
            foreach (GeoPoint wpt in waypoints)
            {
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
                        // todo: maybe add some numeric/precision similarity instead of equality

                        // if reality we are closer to the end of the segment, update what track node we have in mind
                        if (crosspoint == closest_track_node.Next.Point)
                            closest_track_node = closest_track_node.Next;
                        else if (crosspoint != closest_track_node.Point)
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

        private static IReadOnlyDictionary<GeoPoint, List<(TrackNode sibling,Length distance)>> aggregateNodeSiblings(IEnumerable<Track> tracks)
        {
            var dict = new Dictionary<GeoPoint, List<(TrackNode sibling, Length distance)>>();

            foreach (Track trk in tracks)
                foreach (TrackNode node in trk.Nodes)
                {
                    if (!dict.TryGetValue(node.Point, out List<(TrackNode sibling, Length distance)> list))
                    {
                        list = new List<(TrackNode sibling, Length distance)>();
                        dict.Add(node.Point, list);
                    }
                    list.AddRange(node.MeasuredSiblings);
                }

            return dict;
        }

        private static void splitTracksByCrossroad(IEnumerable<Crossroad> crossroads)
        {
            var tracks_to_crossroads = new Dictionary<TrackNode, List<(Length dist, Crossroad crossroad)>>();
            // first we compute all the projections that go per each segmentof the track
            foreach (Crossroad cx in crossroads)
            {
                cx.UpdateProjections();

                foreach (var proj in cx.Projections)
                {
                    if (!tracks_to_crossroads.TryGetValue(proj.node, out List<(Length, Crossroad)> cx_list))
                    {
                        cx_list = new List<(Length, Crossroad)>();
                        tracks_to_crossroads.Add(proj.node, cx_list);
                    }
                    cx_list.Add((proj.nodeProjDistance, cx));
                }
            }

            // now we know into how many pieces each segment should be split
            foreach (var entry in tracks_to_crossroads)
            {
                TrackNode current_node = entry.Key;

                foreach ((Length dist, Crossroad cx) in entry.Value.OrderBy(it => it.dist))
                {
                    current_node = current_node.Add(cx.Point);
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
