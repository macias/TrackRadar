using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Collections;

namespace TrackRadar.Implementation
{
    public sealed partial class GpxLoader
    {
        private sealed class OBSOLETE_NodeQueue
        {
            // length to the nearest turn point : track point
            private PairingHeapNode<Length, TurnNodeInfo> heapRoot;
            // track node -> its heap node
            // null in the value indicates such node was already processed (so do NOT reintroduce it)
            private readonly Dictionary<TrackNode, PairingHeapNode<Length, TurnNodeInfo>> dict;

            public IEnumerable<(TrackNode, GeoPoint)> NodeTurnPoints => this.dict.Select(it => (it.Key, it.Value.Tag.TurnPoint));

            public OBSOLETE_NodeQueue()
            {
                this.dict = new Dictionary<TrackNode, PairingHeapNode<Length, TurnNodeInfo>>();
            }

            public bool TryPop(out Length current_dist, out TrackNode track_node, out GeoPoint turn_pt, out int hops)
            {
                if (heapRoot == null)
                {
                    current_dist = default;
                    track_node = default;
                    turn_pt = default;
                    hops = default;
                    return false;
                }

                current_dist = heapRoot.Weight;
                (track_node, turn_pt, hops) = heapRoot.Tag;
                heapRoot.Pop(ref heapRoot);

                dict[track_node] = null;

                return true;
            }

            public bool Contains(TrackNode node)
            {
                return dict.ContainsKey(node);
            }

            public bool TryGetTurnPoint(TrackNode node, out GeoPoint turnPoint)
            {
                if (!dict.TryGetValue(node, out PairingHeapNode<Length, TurnNodeInfo> heap_node))
                {
                    turnPoint = default;
                    return false;
                }

                turnPoint = heap_node.Tag.TurnPoint;
                return true;
            }

            public void Update(TrackNode trackNode, GeoPoint turnPoint, int hops, Length distance)
            {
                PairingHeapNode<Length, TurnNodeInfo> node;
                if (!dict.TryGetValue(trackNode, out node))
                {
                    node = PairingHeap.Add(ref heapRoot, distance, new TurnNodeInfo(trackNode, turnPoint, hops));
                    dict.Add(trackNode, node);
                }
                else if (node != null && distance < node.Weight)
                {
                    heapRoot.DecreaseWeight(ref heapRoot, node, distance, new TurnNodeInfo(trackNode, turnPoint, hops));
                }
            }

        }

        private sealed class NodeQueue
        {
            private readonly MappedPairingHeap<Length, TurnNodeInfo, TrackNode> mappedHeap;
            private readonly HashSet<TrackNode> usedNodes;

            public IEnumerable<(TrackNode, GeoPoint)> NodeTurnPoints => this.mappedHeap.Tags.Select(it => (it.Node, it.TurnPoint));

            public NodeQueue()
            {
                this.mappedHeap = new MappedPairingHeap<Length, TurnNodeInfo, TrackNode>(info => info.Node);
                this.usedNodes = new HashSet<TrackNode>();
            }

            public bool TryPop(out Length current_dist, out TrackNode track_node, out GeoPoint turn_pt, out int hops)
            {
                if (!this.mappedHeap.TryPop(out current_dist, out TurnNodeInfo info))
                {
                    track_node = default;
                    turn_pt = default;
                    hops = default;
                    return false;
                }

                (track_node, turn_pt, hops) = (info.Node, info.TurnPoint, info.Hops);

                usedNodes.Add(track_node);

                return true;
            }

            public bool Contains(TrackNode node)
            {
                return usedNodes.Contains(node) || mappedHeap.Contains(node);
            }

            public bool TryGetTurnPoint(TrackNode node, out GeoPoint turnPoint)
            {
                if (usedNodes.Contains(node))
                    throw new ArgumentException("Node already used.");

                if (!mappedHeap.TryGetTagValue(node, out TurnNodeInfo info))
                {
                    turnPoint = default;
                    return false;
                }

                turnPoint = info.TurnPoint;
                return true;

            }

            public bool Update(TrackNode trackNode, GeoPoint turnPoint, int hops, Length distance)
            {
                if (usedNodes.Contains(trackNode))
                    return false;

                return mappedHeap.TryAddOrUpdate(distance, new TurnNodeInfo(trackNode, turnPoint, hops));
            }

        }
    }
}