using Gpx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class PlainTile<T> : ITile<T>
         where T : ISegment
    {
        public IEnumerable<T> Segments => this.map;

        // latitude (vertical angle, north-south) -> longitude
        private readonly IReadOnlyList<T> map;

        /// <param name="segments">half-data, pass only A-B form, not A-B and B-A</param>
        public PlainTile(IEnumerable<T> segments)
        {
            this.map = segments.ToArray();
        }

        public bool FindCloseEnough<P>(P point, Length limit, ref T nearby, ref Length distance)
            where P : IGeoPoint
        {
            return find(point, limit, ref nearby, ref distance, returnFirst: false);
        }

        public bool IsWithinLimit<P>(P point, Length limit, out Length distance)
            where P : IGeoPoint
        {
            distance = Length.MaxValue;
            T nearby = default(T);

            return find(point, limit, ref nearby, ref distance, returnFirst: true);
        }

        private bool find<P>(P point, Length limit, ref T nearby, ref Length bestDistance, bool returnFirst)
            where P : IGeoPoint
        {
            if (this.map.Count == 0)
                return false;

            foreach (T segment in this.map)
            {
                Length dist = point.GetDistanceToArcSegment(segment.A, segment.B);

                if (dist < bestDistance || (dist == bestDistance && segment.CompareImportance(nearby) == Ordering.Greater))
                {
                    bestDistance = dist;
                    nearby = segment;
                }

                if (bestDistance <= limit && returnFirst)
                {
                    return true;
                }

            }

            return false;
        }
    }
}