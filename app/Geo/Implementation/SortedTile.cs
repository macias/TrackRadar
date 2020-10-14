using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class SortedTile : ITile
    {
        public IEnumerable<ISegment> Segments => this.map.Values.SelectMany(lon => lon.Values.SelectMany(seg => seg)).Distinct();

        // latitude (vertical angle, north-south) -> longitude
        private readonly SortedList<Angle, SortedList<Angle, List<ISegment>>> map;

        /// <param name="segments">half-data, pass only A-B form, not A-B and B-A</param>
        public SortedTile(IEnumerable<ISegment> segments)
        {
            this.map = new SortedList<Angle, SortedList<Angle, List<ISegment>>>(build(segments));
        }

        private static Dictionary<Angle, SortedList<Angle, List<ISegment>>> build(IEnumerable<ISegment> segments)
        {
            // insertion for SortedList is slow, O(n), so at top-level we initially use Dictionary to populate
            // entries all at once, but for lower level we gamble that the the number of entries is pretty low
            // so we can use SortedList already, for map of my county
            // Dictionary<Dictionary<...>> and transforming it to SortedList<SortedList<...>> gave 7 seconds
            // Dictionary<SortedList<...>> and pushing it directly to constructor (current approach) gave 4 seconds

            var map = new Dictionary<Angle, SortedList<Angle, List<ISegment>>>();

            foreach (ISegment seg in segments)
            {
                getTargets(map, seg.A).Add(seg);
                getTargets(map, seg.B).Add(seg);
            }

            return map;
        }

        private static List<ISegment> getTargets(Dictionary<Angle, SortedList<Angle, List<ISegment>>> map, in GeoPoint point)
        {
            if (!map.TryGetValue(point.Latitude, out SortedList<Angle, List<ISegment>> lon))
            {
                lon = new SortedList<Angle, List<ISegment>>();
                map.Add(point.Latitude, lon);
            }

            if (!lon.TryGetValue(point.Longitude, out List<ISegment> targets))
            {
                targets = new List<ISegment>();
                lon.Add(point.Longitude, targets);
            }

            return targets;
        }

        private static IEnumerable<int> sortedIndices(IList<Angle> list, Angle angle)
        {
            int right = bestStartingIndexOf(list, 0, list.Count - 1, angle);
            yield return right;
            int left = right - 1;
            ++right;

            while (left >= 0 || right < list.Count)
            {
                if (left < 0)
                {
                    yield return right;
                    ++right;
                }
                else if (right == list.Count || (angle - list[left]).Abs() < (angle - list[right].Abs()))
                {
                    yield return left;
                    --left;
                }
                else
                {
                    yield return right;
                    ++right;
                }
            }
        }

        private static int bestStartingIndexOf(IList<Angle> list, int idxLeft, int idxRight, Angle angle)
        {
            Angle left = list[idxLeft];
            Angle right = list[idxRight];
            // since we don't know the data we can bet on having linear values
            int idx = (int)Math.Round(idxLeft + (idxRight - idxLeft) * (angle - left) / (right - left));
            idx = Math.Min(idxRight, Math.Max(idxLeft, idx));

            Angle dist = (angle - list[idx]).Abs();
            if (idx < idxRight && dist > (angle - list[idx + 1]).Abs())
                return bestStartingIndexOf(list, idx + 1, idxRight, angle);
            else if (idx > idxLeft && dist > (angle - list[idx - 1]).Abs())
                return bestStartingIndexOf(list, idxLeft, idx - 1, angle);
            else
                return idx;
        }

        public bool FindCloseEnough(in GeoPoint point, Length limit, ref ISegment nearby, ref Length? distance, 
            out ArcSegmentIntersection crosspointInfo)
        {
            return find(point, limit, ref nearby, ref distance, out crosspointInfo, returnFirst: true);
        }

        /*public bool FindClosest(in GeoPoint point, ref ISegment nearby, ref Length? distance, out GeoPoint crosspoint)
        {
            return find(point, limit: Length.Zero, nearby: ref nearby, bestDistance: ref distance,out crosspoint, returnFirst: true);
        }*/

        public bool IsWithinLimit(in GeoPoint point, Length limit, out Length? distance)
        {
            distance = null;
            ISegment nearby = default(ISegment);

            return find(point, limit, ref nearby, ref distance, out ArcSegmentIntersection _, returnFirst: true);
        }

        private bool find(in GeoPoint point, Length limit, ref ISegment nearby, ref Length? bestDistance, 
            out ArcSegmentIntersection crosspointInfo,            bool returnFirst)
        {
            crosspointInfo = default;

            if (this.map.Count == 0)
                return false;

            var visited = new HashSet<ISegment>();

            bool found = false;

            foreach (var lat_idx in sortedIndices(this.map.Keys, point.Latitude))
            {
                foreach (var lon_idx in sortedIndices(this.map.Values[lat_idx].Keys, point.Longitude))
                {
                    foreach (ISegment segment in this.map.Values[lat_idx].Values[lon_idx])
                    {
                        if (!visited.Add(segment))
                            continue;

                        Length dist = point.GetDistanceToArcSegment(segment.A, segment.B,out ArcSegmentIntersection cx);

                        if (bestDistance==null || dist < bestDistance || (dist == bestDistance && segment.CompareImportance(nearby) == Ordering.Greater))
                        {
                            bestDistance = dist;
                            nearby = segment;
                            crosspointInfo = cx;
                            found = true;
                        }

                        // if we are below limit with our best
                        // and either we have to return fast, 
                        // or we crossed the limits (but we already found something good) and we know it won't be better
                        // return
                        if (bestDistance <= limit && (returnFirst || dist > limit))
                        {
                            return true;
                        }

                    }
                }
            }

            return found && limit == Length.Zero;
        }

        public IEnumerable<IMeasuredPinnedSegment> FindAll( GeoPoint point, Length limit)
        {
            foreach (ISegment segment in this.Segments)
            {
                Length dist = point.GetDistanceToArcSegment(segment.A, segment.B, out GeoPoint cx);

                if (dist <= limit)
                {
                    yield return MeasuredPinnedSegment.Create(cx, segment, dist);
                }
            }
        }
    }
}