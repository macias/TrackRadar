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
        private sealed class NodeQueue
        {
            private readonly MappedPairingHeap<TrackNode,Length, TurnNodeInfo> mappedHeap;
            private readonly HashSet<TrackNode> usedNodes;

            public IEnumerable<(TrackNode, GeoPoint)> NodeTurnPoints => this.mappedHeap.Data.Select(it => (it.key, it.value.TurnPoint));

            public NodeQueue()
            {
                this.mappedHeap = MappedPairingHeap.Create<TrackNode, Length, TurnNodeInfo>();
                this.usedNodes = new HashSet<TrackNode>();
            }

            public bool TryPop(out Length current_dist, out TrackNode track_node, out GeoPoint turn_pt, out int hops)
            {
                if (!this.mappedHeap.TryPop(out track_node, out current_dist, out var info))
                {
                    track_node = default;
                    turn_pt = default;
                    hops = default;
                    return false;
                }

                (turn_pt, hops) = (info.TurnPoint, info.Hops);

                usedNodes.Add(track_node);

                return true;
            }

            public bool Contains(TrackNode node)
            {
                return usedNodes.Contains(node) || mappedHeap.ContainsKey(node);
            }

            public bool TryGetTurnPoint(TrackNode node, out GeoPoint turnPoint)
            {
                if (usedNodes.Contains(node))
                    throw new ArgumentException("Node already used.");

                if (!mappedHeap.TryGetData(node, out _, out TurnNodeInfo info))
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

                return mappedHeap.TryAddOrUpdate(trackNode, distance, new TurnNodeInfo(turnPoint, hops));
            }

        }
    }
}