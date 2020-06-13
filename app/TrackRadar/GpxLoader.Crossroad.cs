using Geo;
using Gpx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar
{
    public partial class GpxLoader
    {
        internal sealed class TrackIndex
        {
            public int First { get; }
            public int Second { get; }

            public TrackIndex(int first, int second)
            {
                First = Math.Min(first, second);
                Second = Math.Max(first, second);
            }

            public override bool Equals(object obj)
            {
                if (obj is TrackIndex idx)
                    return Equals(idx);
                else
                    return false;
            }

            public bool Equals(TrackIndex other)
            {
                if (ReferenceEquals(other, null))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                if (this.GetType() != other.GetType())
                    return false;

                return this.First == other.First && this.Second == other.Second;
            }

            public override int GetHashCode()
            {
                return First.GetHashCode() ^ Second.GetHashCode();
            }
        }
        // todo: private
        internal sealed class Crossroad
        {
#if DEBUG
            private static int debugId;
            public int DebugId { get; } = debugId++;
#endif

            public NodePoint Node { get; }

            internal CrossroadKind Kind { get { return this.Node.Kind; } set { this.Node.Kind = value; } }
            public TrackIndex SourceIndex { get; internal set; }

            public Crossroad(GeoPoint cx)
            {
                this.Node = new NodePoint() { Point = cx, Kind = CrossroadKind.Intersection };
            }
            public Crossroad(Crossroad a, Crossroad b)
            {
                this.Node = new NodePoint(a.Node, b.Node);
            }

            public override string ToString()
            {
                return $"{SourceIndex} {Kind}";
            }

            internal Crossroad Connected(NodePoint other)
            {
                this.Node.Connect(other);
                return this;
            }

            internal void Disconnect()
            {
                this.Node.Disconnect();
            }
        }
    }
}
