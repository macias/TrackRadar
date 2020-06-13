using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class PlainTile : ITile
    {
        public IEnumerable<ISegment> Segments => this.map;

        // latitude (vertical angle, north-south) -> longitude
        private readonly IReadOnlyList<ISegment> map;

        /// <param name="segments">half-data, pass only A-B form, not A-B and B-A</param>
        public PlainTile(IEnumerable<ISegment> segments)
        {
            this.map = segments.ToArray();
        }

        public bool FindCloseEnough(in Geo.GeoPoint point, Length limit, ref ISegment nearby, ref Length? distance)
        {
            return find(point, limit, ref nearby, ref distance, returnFirst: false);
        }

        public bool FindClosest(in Geo.GeoPoint point, ref ISegment nearby, ref Length? distance)
        {
            return find(point, Length.Zero, ref nearby, ref distance, returnFirst: false);
        }

        public bool IsWithinLimit(in Geo.GeoPoint point, Length limit, out Length? distance)
        {
            distance = null;
            ISegment nearby = default(ISegment);

            return find(point, limit, ref nearby, ref distance, returnFirst: true);
        }

        private bool find(in Geo.GeoPoint point, Length limit, ref ISegment nearby, ref Length? bestDistance, bool returnFirst)
        {
            bool found = false;

            foreach (ISegment segment in this.map)
            {
                Length dist = point.GetDistanceToArcSegment(segment.A, segment.B);

                if (bestDistance==null || dist < bestDistance || (dist == bestDistance && segment.CompareImportance(nearby) == Ordering.Greater))
                {
                    bestDistance = dist;
                    nearby = segment;
                    found = true;
                }

                if (bestDistance <= limit && returnFirst)
                {
                    return true;
                }

            }

            return found && limit== Length.Zero;
        }

        public IEnumerable<IMeasuredPinnedSegment> FindAll( Geo.GeoPoint point, Length limit)
        {
            foreach (ISegment segment in this.map)
            {
                Length dist = point.GetDistanceToArcSegment(segment.A, segment.B,out Geo.GeoPoint cx);

                if (dist <= limit)
                {
                    yield return MeasuredPinnedSegment.Create(cx, segment,dist);
                }
            }
        }

    }
}