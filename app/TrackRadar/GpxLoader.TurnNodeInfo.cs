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
        private readonly struct TurnNodeInfo
        {
            public TrackNode Node { get; }
            public GeoPoint TurnPoint { get; }
            // 0 for immediate connection between turn and node, 1 when node has to go through immediate node, etc.
            public int Hops { get; } 

            public TurnNodeInfo(TrackNode node,GeoPoint turnPoint,int hops)
            {
                Node = node;
                TurnPoint = turnPoint;
                Hops = hops;
            }

            public void Deconstruct(out TrackNode node, out GeoPoint turnPoint, out int hops)
            {
                node = this.Node;
                turnPoint = this.TurnPoint;
                hops = this.Hops;
            }
        }
      }
}