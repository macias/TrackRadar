using Geo;
using MathUnit;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Collections;

namespace TrackRadar
{
    public sealed partial class GpxLoader
    {    
        private sealed class NodeQueue
        {
            // length to the nearest turn point : track point
            private PairingHeapNode<Length, TurnNodeInfo> heapRoot;
            // track node -> its heap node
            // null in the value indicates such node was already processed (so do NOT reintroduce it)
            private readonly Dictionary<TrackNode, PairingHeapNode<Length, TurnNodeInfo>> heapNodes;

            public IEnumerable<(TrackNode,GeoPoint)> NodeTurnPoints => this.heapNodes.Select(it => (it.Key,it.Value.Tag.TurnPoint));

            public NodeQueue()
            {
                this.heapNodes = new Dictionary<TrackNode, PairingHeapNode<Length, TurnNodeInfo>>();
            }

            public bool TryPop(out Length current_dist, out TrackNode track_node, out GeoPoint turn_pt,out int hops)
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
                (track_node, turn_pt,hops) = heapRoot.Tag;
                heapRoot.Pop(ref heapRoot);

                heapNodes[track_node] = null;

                return true;
            }

            public bool Contains(TrackNode node)
            {
                return heapNodes.ContainsKey(node);
            }

            public bool TryGetTurnPoint(TrackNode node,out GeoPoint turnPoint)
            {
                if (!heapNodes.TryGetValue(node, out PairingHeapNode<Length, TurnNodeInfo> heap_node))
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
                if (!heapNodes.TryGetValue(trackNode, out node))
                {
                    node = PairingHeap.Add(ref heapRoot, distance, new TurnNodeInfo(trackNode,turnPoint,hops));
                    heapNodes.Add(trackNode, node);
                }
                else if (node != null && distance < node.Weight)
                {
                    heapRoot.DecreaseWeight(ref heapRoot, node, distance, new TurnNodeInfo(trackNode, turnPoint, hops));
                }
            }

        }

    }
}