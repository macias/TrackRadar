using Gpx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar
{
    internal partial class GpxLoader
    {
        private sealed class Crossroad
        {
            public NodePoint Node { get; }

            public CrossroadKind Kind { get { return this.Node.Kind; } set { this.Node.Kind = value; } }
            public Tuple<int, int> SourceIndex { get; internal set; }

            // same crossroads lie outside tracks, but some are located right at tracks
            // so we need to know on which tracks
            private readonly List<List<NodePoint>> insertionTracks;
            public bool IsInserted => this.insertionTracks.Any();

            public Crossroad(IGeoPoint cx)
            {
                this.Node = new NodePoint() { Point = cx, Kind = CrossroadKind.Intersection };
                this.insertionTracks = new List<List<NodePoint>>();
            }
            public Crossroad(Crossroad a, Crossroad b)
            {
                this.insertionTracks = new List<List<NodePoint>>();

                this.Node = new NodePoint(a.Node, b.Node);

                // let's update insertion point
                foreach (Crossroad cr in new[] { a, b })
                {
                    foreach (List<NodePoint> track in cr.insertionTracks)
                    {
                        if (this.insertionTracks.Contains(track))
                            continue;

                        this.insertionTracks.Add(track);

                        int idx = track.FindIndex(it => it == cr.Node);
                        if (idx == -1)
                            throw new IndexOutOfRangeException("Cannot find insertion point");

                        track[idx] = this.Node;
                    }
                }
            }

            public override string ToString()
            {
                return $"{SourceIndex} {Kind}";
            }

            internal void RegisterInsertion(List<NodePoint> track)
            {
                this.insertionTracks.Add(track);
            }

            internal Crossroad Connected(NodePoint other)
            {
                this.Node.Connect(other);
                return this;
            }
        }
    }
}
