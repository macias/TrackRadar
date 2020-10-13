using Geo;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar
{
    public partial class GpxLoader
    {
        internal sealed class Track
        {
#if DEBUG
            public int DEBUG_Count => this.Nodes.Count();
            public bool DEBUG_AllNodesSectionSet => this.Nodes.All(it => it.IsSectionSet);
#endif
            public TrackNode Head { get; }

            public IEnumerable<TrackNode> Nodes => this.Head.Nodes;

            public IEnumerable<GeoPoint> GeoPoints => this.Nodes.Select(it => it.Point);

            public Track(IEnumerable<GeoPoint> points)
            {
                this.Head = build(points);
            }

            private static TrackNode build(IEnumerable<GeoPoint> points)
            {
                TrackNode prev = null;
                TrackNode root = null;
                foreach (GeoPoint pt in points)
                {
                    if (prev == null)
                    {
                        prev = new GpxLoader.TrackNode(pt, null, null);
                        root = prev;
                    }
                    else
                        prev = prev.Add(pt);
                }

                return root;
            }
        }
    }
}
