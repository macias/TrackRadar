using System;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    public partial class GpxLoader
    {
        internal readonly struct WayPointInfo
        {
            private readonly HashSet<TrackNode> neighbours;
            public WayPointKind Kind { get; }
            public IEnumerable<TrackNode> Neighbours => this.neighbours;

            public WayPointInfo(WayPointKind kind)
            {
                this.Kind = kind;
                this.neighbours = new HashSet<TrackNode>();
            }

            internal void AddNeighbours(IEnumerable<TrackNode> neighbours)
            {
                this.neighbours.AddRange(neighbours);
            }
        }
        
    }
}
