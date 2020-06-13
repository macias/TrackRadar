using Geo.Comparers;
using Geo.Implementation.Comparers;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class Grid : IGeoMap
    {
        // longitude: ranging from 0° at the Prime Meridian to +180° eastward and −180° westward
        // latitude: The Equator has a latitude of 0°, the North Pole has a latitude of +90°, and the South Pole −90°

        // the idea of this approach is to divide entire map into tiles, so when we start searching for given point
        // we would have much more data to process (only few tiles instead of entire map)
        // the only tricky part is to remember that the tiles are not squares, only trapezoids
        // for northern part of the globe:
        //    ---
        //   /   \
        //  -------

        private readonly int longitudeCount;
        private readonly int latitudeCount;
        private readonly Length longitudeTileLength;
        private readonly Length latitudeTileLength;
        private readonly IReadOnlyList<ITile> tiles;
        private readonly Angle westmost, eastmost, northmost, southmost;
        private readonly IGraph graph;

        public IEnumerable<Geo.ISegment> Segments => this.tiles.SelectMany(it => it.Segments).Distinct();

        public Grid(IEnumerable<Geo.ISegment> segments)
        {
            this.graph = new Graph(segments.Select(it => it.Points()));

            if (!MapCalculator.TryGetBoundaries(segments.SelectMany(it => it.Points()),
                out westmost, out eastmost, out northmost, out southmost))
            {
                this.tiles = new List<ITile>();
            }
            else
            {
                Length lat_len, lon_len;
                {
                    // we are taking the shortest length along longitude
                    Angle lat = southmost.Abs().Max(northmost.Abs());
                    lon_len = GeoCalculator.GetDistance(new Geo.GeoPoint(longitude: westmost, latitude: lat),
                        new Geo.GeoPoint(longitude: eastmost, latitude: lat));
                }

                lat_len = GeoCalculator.GetDistance(new Geo.GeoPoint(longitude: westmost, latitude: northmost),
                        new Geo.GeoPoint(longitude: westmost, latitude: southmost));

                // the idea is to split entire map in such tiles, that the longest segment could fit entirely in any of the tiles
                // (as a guarantee), so later we would know that each end is in this or adjacent tile, why this is important
                // well, if otherwise selecting given tile could not bring important segment, because it could span over several ones
                //  A---|----|-*-|---|---B
                // * we look here, A and B the ends of the segment, from * perspective, AB is non existent

                Length seg_len = segments.Max(it => GeoCalculator.GetDistance(it.A, it.B));

                this.longitudeCount = Math.Max(1, (int)Math.Floor(lon_len / seg_len));
                this.latitudeCount = Math.Max(1, (int)Math.Floor(lat_len / seg_len));

                this.longitudeTileLength = lon_len / this.longitudeCount;
                this.latitudeTileLength = lat_len / this.latitudeCount;

                List<List<ISegment>> buckets = Enumerable.Range(0, longitudeCount * latitudeCount)
                    .Select(_ => new List<ISegment>())
                    .ToList();

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
                this.tiles = buckets.Select(it => new PlainTile(it)).ToList();
            }

        }

        private int getTileIndex(in Geo.GeoPoint point)
        {
            if (!tryGetTileIndices(point, out int lonIndex, out int latIndex))
                throw new ArgumentOutOfRangeException();
            return combineIndices(lonIndex, latIndex);
        }

        private int combineIndices(int lonIndex, int latIndex)
        {
            return latIndex * this.longitudeCount + lonIndex;
        }

        private bool tryGetTileIndices(in Geo.GeoPoint point, out int lonIndex, out int latIndex)
        {
            if (this.longitudeCount == 0) // we had no points as input
            {
                lonIndex = 0;
                latIndex = 0;
                return false;
            }

            lonIndex = (int)Math.Floor((this.longitudeCount - 1) * (point.Longitude - westmost) / (eastmost - westmost));
            latIndex = (int)Math.Floor((this.latitudeCount - 1) * (point.Latitude - southmost) / (northmost - southmost));

            if (lonIndex < 0 || lonIndex >= this.longitudeCount || latIndex < 0 || latIndex >= this.latitudeCount)
            {
                return false;
            }

            return true;
        }

        public bool FindCloseEnough(in Geo.GeoPoint point, Length limit, out ISegment nearby, out Length? distance)

        {
            distance = null;
            nearby = default(ISegment);

            bool result = false;
            foreach (ITile tile in getTiles(point, limit))
                if (tile.FindCloseEnough(point, limit, ref nearby, ref distance))
                    result = true;

            return result;
        }

        public bool FindClosest(in Geo.GeoPoint point, out ISegment nearby, out Length? distance)
        {
            distance = null;
            nearby = default(ISegment);

            foreach (ITile tile in getTiles(point, Length.Zero))
            {
                if (tile.FindClosest(point, ref nearby, ref distance))
                    return true;
            }

            return false;
        }

        public bool IsWithinLimit(in Geo.GeoPoint point, Length limit, out Length? distance)
        {
            distance = null;
            foreach (ITile tile in getTiles(point, limit))
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

        private IEnumerable<ITile> getTiles(in Geo.GeoPoint point, Length limit)
        {
            tryGetTileIndices(point, out int lonIndex, out int latIndex);

            int lon_count = 1 + (int)Math.Floor(limit / this.longitudeTileLength);
            int lat_count = 1 + (int)Math.Floor(limit / this.latitudeTileLength);

            var indices = new List<Tuple<int, int>>();
            for (int lon = lonIndex - lon_count; lon <= lonIndex + lon_count; ++lon)
            {
                for (int lat = latIndex - lat_count; lat <= latIndex + lat_count; ++lat)
                {
                    if (lon >= 0 && lat >= 0 && lon < this.longitudeCount && lat < this.latitudeCount)
                        indices.Add(Tuple.Create(lon, lat));
                }
            }

            // starting from the center of the region
            return indices
                .OrderBy(it => (it.Item1 - lonIndex) * (it.Item1 - lonIndex) + (it.Item2 - latIndex) * (it.Item2 - latIndex))
                .Select(it => this.tiles[combineIndices(it.Item1, it.Item2)]);
        }

        public IEnumerable<IMeasuredPinnedSegment> FindAll(Geo.GeoPoint point, Length limit)
        {
            return getTiles(point, limit).SelectMany(it => it.FindAll(point, limit))
                .Distinct(SegmentPinNumericComparer.Default)
                .Select(it => (IMeasuredPinnedSegment)it);
        }

        public IEnumerable<ISegment> GetNearby(in Geo.GeoPoint point, Length limit)
        {
            return getTiles(point, limit).SelectMany(it => it.Segments)
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

        public IEnumerable<GeoPoint> GetAdjacent(in GeoPoint node)
        {
            return this.graph.GetAdjacent(node);
        }

        /*public GeoPoint GetReference(Angle latitude, Angle longitude)
        {
            return this.graph.GetReference(latitude, longitude);
        }
        */
    }
}