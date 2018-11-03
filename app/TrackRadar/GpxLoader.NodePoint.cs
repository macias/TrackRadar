using Gpx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar
{
    internal partial class GpxLoader
    {
        private sealed class NodePoint
        {
            public IGeoPoint Point { get; set; }
            public CrossroadKind Kind { get; set; }
            // extra info in cases like track-crossroad relation, we need to know which points are adjacent
            public HashSet<NodePoint> Neighbours { get; }

            public NodePoint()
            {
                this.Kind = CrossroadKind.None;
                this.Neighbours = new HashSet<NodePoint>();
            }
            public NodePoint(NodePoint a, NodePoint b) : this()
            {
                this.Kind = CrossroadKind.Intersection;
                this.Point = GeoCalculator.GetMidPoint(a.Point, b.Point);
                foreach (NodePoint neighbour in new[] { a, b }.SelectMany(it => it.Neighbours).ToArray())
                    neighbour.Connect(this);
                a.Disconnect();
                b.Disconnect();
            }

            private void Disconnect()
            {
                foreach (NodePoint neighbour in this.Neighbours)
                    neighbour.Neighbours.Remove(this);
                this.Neighbours.Clear();
            }

            public void ConnectThrough()
            {
                // connects all neighbours one to another
                // this is preparation to remove this point

                foreach (NodePoint neighbour_a in this.Neighbours)
                {
                    neighbour_a.Neighbours.Remove(this);
                    foreach (NodePoint neighbour_b in this.Neighbours.SkipWhile(it => it != neighbour_a).Skip(1))
                        neighbour_a.Connect(neighbour_b);
                }
            }

            internal void Connect(NodePoint other)
            {
                this.Neighbours.Add(other);
                other.Neighbours.Add(this);
            }
        }
    }
}
