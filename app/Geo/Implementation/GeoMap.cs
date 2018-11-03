using Gpx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class GeoMap<T> : IGeoMap<T>
         where T : ISegment
    {
        public IEnumerable<T> Segments => this.map.Values.SelectMany(lon => lon.Values.SelectMany(seg => seg)).Distinct();

        // latitude (vertical angle, north-south) -> longitude
        private readonly SortedList<Angle, SortedList<Angle, List<T>>> map;

        /// <param name="segments">half-data, pass only A-B form, not A-B and B-A</param>
        public GeoMap(IEnumerable<T> segments)
        {
            this.map = new SortedList<Angle, SortedList<Angle, List<T>>>(build(segments));
        }

        private static Dictionary<Angle, SortedList<Angle, List<T>>> build(IEnumerable<T> segments)
        {
            // insertion for SortedList is slow, O(n), so at top-level we initially use Dictionary to populate
            // entries all at once, but for lower level we gamble that the the number of entries is pretty low
            // so we can use SortedList already, for map of my county
            // Dictionary<Dictionary<...>> and transforming it to SortedList<SortedList<...>> gave 7 seconds
            // Dictionary<SortedList<...>> and pushing it directly to constructor (current approach) gave 4 seconds

            var map = new Dictionary<Angle, SortedList<Angle, List<T>>>();

            foreach (T seg in segments)
            {
                getTargets(map, seg.A).Add(seg);
                getTargets(map, seg.B).Add(seg);
            }

            return map;
        }

        private static List<T> getTargets(Dictionary<Angle, SortedList<Angle, List<T>>> map, IGeoPoint point)
        {
            if (!map.TryGetValue(point.Latitude, out SortedList<Angle, List<T>> lon))
            {
                lon = new SortedList<Angle, List<T>>();
                map.Add(point.Latitude, lon);
            }

            if (!lon.TryGetValue(point.Longitude, out List<T> targets))
            {
                targets = new List<T>();
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

        public bool FindCloseEnough<P>(P point, Length limit, out T nearby,out Length distance)
            where P : IGeoPoint
        {
            return find(point, limit, out nearby, out distance, returnFirst: false);
        }

        public bool IsWithinLimit<P>(P point, Length limit, out Length distance)
            where P : IGeoPoint
        {
            return find(point, limit, out _, out distance, returnFirst: true);
        }

        private bool find<P>(P point, Length limit, out T nearby, out Length distance,bool returnFirst)
            where P : IGeoPoint
        {
            var visited = new HashSet<T>();

            // this distance is for reporting to outer world
            distance = Length.MaxValue;
            // this one is for tracking the segment within the limits ONLY
            Length best_distance = Length.MaxValue;
            nearby = default(T);

            foreach (var lat_idx in sortedIndices(this.map.Keys, point.Latitude))
            {
                foreach (var lon_idx in sortedIndices(this.map.Values[lat_idx].Keys, point.Longitude))
                {
                    foreach (T segment in this.map.Values[lat_idx].Values[lon_idx])
                    {
                        if (visited.Contains(segment))
                            continue;

                        visited.Add(segment);

                        Length dist = point.GetDistanceToArcSegment(segment.A, segment.B);
                        distance = distance.Min(dist);

                        if (dist > limit)
                        {
                            // we crossed the limits, if we already found something good, return
                            if (best_distance != Length.MaxValue)
                            {
                                return true;
                            }
                        }
                        else if (dist < best_distance
                            || (segment.IsMoreImportant(nearby) && dist == best_distance))
                        {
                            best_distance = dist;
                            nearby = segment;
                            if (returnFirst)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}