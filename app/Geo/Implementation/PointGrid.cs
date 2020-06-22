using MathUnit;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class PointGrid : Grid<GeoPoint, PointTile>, IPointGrid
    {
        public PointGrid(IEnumerable<GeoPoint> points, Length tileSize) : base(points, x => x, tileSize)
        {
        }

        protected override IReadOnlyList<PointTile> tileBuckets(IEnumerable<GeoPoint> points, List<List<GeoPoint>> buckets)
        {
            foreach (GeoPoint pt in points)
            {
                int idx = getTileIndex(pt);
                buckets[idx].Add(pt);
            }

            // Console.WriteLine($"Median occupancy {buckets.OrderBy(it => it.Count).ToList()[buckets.Count/2].Count}");

            //            this.tiles = buckets.Select(it => new SortedTile<T>(it)).ToList();
            return buckets.Select(it => new PointTile(it)).ToList();

        }

        public IEnumerable<GeoPoint> GetNearby(in Geo.GeoPoint point, Length limit)
        {
            return getTilesCloserThan(point, limit)
                .SelectMany(it => it.Points);
        }

    }
}