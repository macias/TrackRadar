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
        //private readonly IGraph graph;

        public IEnumerable<Geo.ISegment> Segments => this.tiles.SelectMany(it => it.Segments).Distinct();

        public SegmentGrid(IEnumerable<Geo.ISegment> segments) : base(segments, segs => segs.SelectMany(it => it.Points()),
            segments.Any() ? segments.Max(it => GeoCalculator.GetDistance(it.A, it.B)) : Length.Zero)
        {
          //  this.graph = new Graph(segments.Select(it => it.Points()));
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

        public bool FindCloseEnough(in Geo.GeoPoint point, Length limit, out ISegment nearby, out Length? distance)

        {
            distance = null;
            nearby = default(ISegment);

            bool result = false;
            foreach (ITile tile in getTilesCloserThan(point, limit))
                if (tile.FindCloseEnough(point, limit, ref nearby, ref distance))
                    result = true;

            return result;
        }

        public bool FindClosest(in Geo.GeoPoint point, out ISegment nearby, out Length? distance)
        {
            distance = null;
            nearby = default(ISegment);

            foreach (ITile tile in getTilesCloserThan(point, upperLimit: Length.Zero))
            {
                if (tile.FindClosest(point, ref nearby, ref distance))
                    return true;
            }

            return false;
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

        /*public IEnumerable<GeoPoint> GetAdjacent(in GeoPoint node)
        {
            return this.graph.GetAdjacent(node);
        }*/

        /*public GeoPoint GetReference(Angle latitude, Angle longitude)
        {
            return this.graph.GetReference(latitude, longitude);
        }
        */
    }
}