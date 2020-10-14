using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class PlainTile : ITile
    {
        public IEnumerable<ISegment> Segments => this.segments;

        private readonly IReadOnlyList<ISegment> segments;

        /// <param name="segments">half-data, pass only A-B form, not A-B and B-A</param>
        public PlainTile(IEnumerable<ISegment> segments)
        {
            this.segments = segments.ToArray();
        }

        public bool FindCloseEnough(in Geo.GeoPoint point, Length limit, ref ISegment nearby, ref Length? distance, 
            out ArcSegmentIntersection crosspointInfo)
        {
            return find(point, limit, ref nearby, ref distance, out crosspointInfo, returnFirst: true);
        }

        /*public bool FindClosest(in Geo.GeoPoint point, ref ISegment nearby, ref Length? distance, out GeoPoint crosspoint)
        {
            return find(point, limit: Length.Zero, ref nearby, ref distance, out crosspoint, returnFirst: true);
        }*/

        public bool IsWithinLimit(in Geo.GeoPoint point, Length limit, out Length? distance)
        {
            distance = null;
            ISegment nearby = default;

            return find(point, limit, ref nearby, ref distance, out ArcSegmentIntersection _, returnFirst: true);
        }

        private bool find(in Geo.GeoPoint point, Length limit, ref ISegment nearby, ref Length? bestDistance,
            out ArcSegmentIntersection crosspointInfo, bool returnFirst)
        {
            bool found = false;
            crosspointInfo = default;

            foreach (ISegment segment in this.segments)
            {
                Length dist = point.GetDistanceToArcSegment(segment.A, segment.B, out ArcSegmentIntersection cx);

                if (bestDistance == null || dist < bestDistance
                    || (dist == bestDistance && segment.CompareImportance(nearby) == Ordering.Greater))
                {
                    bestDistance = dist;
                    nearby = segment;
                    crosspointInfo = cx;
                    found = true;
                }

                if (bestDistance <= limit && returnFirst)
                {
                    return true;
                }

            }

            return found && limit == Length.Zero;
        }

        public IEnumerable<IMeasuredPinnedSegment> FindAll(Geo.GeoPoint point, Length limit)
        {
            foreach (ISegment segment in this.segments)
            {
                Length dist = point.GetDistanceToArcSegment(segment.A, segment.B, out Geo.GeoPoint cx);

                if (dist <= limit)
                {
                    yield return MeasuredPinnedSegment.Create(cx, segment, dist);
                }
            }
        }

    }
}