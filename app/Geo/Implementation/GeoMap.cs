using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class GeoMap : IGeoMap
    {
        //private readonly IGraph graph;
        private readonly ITile map;

        public IEnumerable<ISegment> Segments => this.map.Segments;

        /// <param name="segments">half-data, pass only A-B form, not A-B and B-A</param>
        public GeoMap(IEnumerable<ISegment> segments)
        {
          //  this.graph = new Graph(segments.Select(it => it.Points()));
            this.map = new SortedTile(segments);
        }

        public bool FindCloseEnough(in GeoPoint point, Length limit, out ISegment nearby, out Length? distance) 
        {
            distance = null;
            nearby = default(ISegment);

            return this.map.FindCloseEnough(point, limit, ref nearby, ref distance);
        }
        public bool FindClosest(in GeoPoint point,out ISegment nearby, out Length? distance) 
        {
            distance = Length.MaxValue;
            nearby = default(ISegment);

            if (!this.map.FindClosest(point, ref nearby, ref distance))
                throw new InvalidOperationException();

            return true;
        }

        public bool IsWithinLimit(in GeoPoint point, Length limit, out Length? distance) 
        {
            return this.map.IsWithinLimit(point, limit, out distance);
        }

        public IEnumerable<IMeasuredPinnedSegment> FindAll( GeoPoint point, Length limit) 
        {
            return this.map.FindAll(point, limit);
        }

        public IEnumerable<ISegment> GetNearby(in GeoPoint point, Length limit) 
        {
            return this.map.Segments;
        }

        private static bool isWithinRegion(in GeoPoint p, Angle westmost, Angle eastmost, Angle northmost, Angle southmost)
        {
            return p.Longitude >= westmost && p.Longitude <= eastmost && p.Latitude <= northmost && p.Latitude >= southmost;
        }

        public IEnumerable<ISegment> GetFromRegion(Angle westmost, Angle eastmost, Angle northmost, Angle southmost)
        {
            foreach (var seg in Segments)
                if (isWithinRegion(seg.A, westmost, eastmost, northmost, southmost)
                    || isWithinRegion(seg.B, westmost, eastmost, northmost, southmost))
                    yield return seg;
        }

        /*public IEnumerable<GeoPoint> GetAdjacent(in GeoPoint node)
        {
            return this.graph.GetAdjacent(node);
        }
        */
        /*public GeoPoint GetReference(Angle latitude, Angle longitude)
        {
            return this.graph.GetReference(latitude, longitude);
        }*/

    }
}