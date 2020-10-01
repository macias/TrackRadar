using Geo.Comparers;
using Geo.Implementation.Comparers;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class SegmentGrid : Grid<ISegment, ITile>, IGeoMap
    {
        public IEnumerable<Geo.ISegment> Segments => this.tiles.SelectMany(it => it.Segments).Distinct();

        public SegmentGrid(IEnumerable<Geo.ISegment> segments) : base(segments, segs => segs.SelectMany(it => it.Points()),
            segments.Any() ? segments.Max(it => it.GetLength()) : Length.Zero)
        {
        }

        protected override IReadOnlyList<ITile> tileBuckets(IEnumerable<ISegment> segments, List<List<ISegment>> buckets)
        {
            foreach (ISegment seg in segments)
            {
                int a_idx = getTileIndex(seg.A);
                int b_idx = getTileIndex(seg.B);
                buckets[a_idx].Add(seg);
                if (a_idx != b_idx)
                    buckets[b_idx].Add(seg);
            }

            // Console.WriteLine($"Median occupancy {buckets.OrderBy(it => it.Count).ToList()[buckets.Count/2].Count}");

            //            this.tiles = buckets.Select(it => new SortedTile<T>(it)).ToList();
            return buckets.Select(it => new PlainTile(it)).ToList();
        }

        public bool FindCloseEnough(in Geo.GeoPoint point, Length limit, out ISegment nearby, out Length? distance, out GeoPoint crosspoint)
        {
            distance = null;
            nearby = default(ISegment);
            crosspoint = default;

            bool result = false;
            foreach (ITile tile in getTilesCloserThan(point, limit))
                if (tile.FindCloseEnough(point, limit, ref nearby, ref distance, out GeoPoint cx))
                {
                    crosspoint = cx;
                    result = true;
                    break;
                }

            return result;
        }

        public bool FindClosest(in Geo.GeoPoint point, Length? limit, out ISegment nearby, out Length? distance, out GeoPoint crosspoint)
        {
            distance = null;
            nearby = default(ISegment);
            crosspoint = default;
            bool result = false;

            foreach (ITile tile in getTilesCloserThan(point, upperLimit: limit?? Length.Zero))
            {
                if (tile.FindClosest(point, ref nearby, ref distance, out GeoPoint cx))
                {
                    result = true;
                    crosspoint = cx;
                    if (distance == Length.Zero)
                        break;
                }
            }

            return result && distance <= (limit ?? Length.MaxValue);
        }

        public bool IsWithinLimit(in Geo.GeoPoint point, Length limit, out Length? distance)
        {
            distance = null;
            foreach (ITile tile in getTilesCloserThan(point, limit))
            {
                if (tile.IsWithinLimit(point, limit, out Length? tile_dist))
                {
                    distance = tile_dist;
                    return true;
                }
                else if (distance == null || tile_dist < distance)
                    distance = tile_dist;
            }

            return false;
        }
       
        public IEnumerable<IMeasuredPinnedSegment> FindAll(Geo.GeoPoint point, Length limit)
        {
            return getTilesCloserThan(point, limit).SelectMany(it => it.FindAll(point, limit))
                .Distinct(SegmentPinNumericComparer.Default)
                .Select(it => (IMeasuredPinnedSegment)it);
        }

        public IEnumerable<ISegment> GetNearby(in Geo.GeoPoint point, Length limit)
        {
            return getTilesCloserThan(point, limit).SelectMany(it => it.Segments)
                .Distinct(ReferenceComparer<ISegment>.Default);
        }


        private static bool isWithinRegion(in Geo.GeoPoint p, Angle westmost, Angle eastmost, Angle northmost, Angle southmost)
        {
            return p.Longitude >= westmost && p.Longitude <= eastmost && p.Latitude <= northmost && p.Latitude >= southmost;
        }

        public IEnumerable<Geo.ISegment> GetFromRegion(Angle westmost, Angle eastmost, Angle northmost, Angle southmost)
        {
            foreach (var seg in Segments)
                if (isWithinRegion(seg.A, westmost, eastmost, northmost, southmost)
                    || isWithinRegion(seg.B, westmost, eastmost, northmost, southmost))
                    yield return seg;
        }
    }
}