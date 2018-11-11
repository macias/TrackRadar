using Gpx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class Grid<T> : IGeoMap<T>
        where T : ISegment
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
        private readonly IReadOnlyList<ITile<T>> tiles;
        private readonly Angle mostWest, mostEast, mostNorth, mostSouth;

        public IEnumerable<T> Segments => this.tiles.SelectMany(it => it.Segments).Distinct();

        public Grid(IEnumerable<T> segments)
        {
            Length seg_len;
            computeBoundaries(segments, out seg_len, out mostWest, out mostEast, out mostNorth, out mostSouth);

            Length lat_len, lon_len;
            {
                // we are taking the shortest length along longitude
                Angle lat = mostSouth.Abs().Max(mostNorth.Abs());
                lon_len = new GeoPoint() { Longitude = mostWest, Latitude = lat }
                    .GetDistance(new GeoPoint() { Longitude = mostEast, Latitude = lat });
            }

            lat_len = new GeoPoint() { Longitude = mostWest, Latitude = mostNorth }
                    .GetDistance(new GeoPoint() { Longitude = mostWest, Latitude = mostSouth });

            // the idea is to split entire map in such tiles, that the longest segment could fit entirely in any of the tiles
            // (as a guarantee), so later we would know that each end is in this or adjacent tile, why this is important
            // well, if otherwise selecting given tile could not bring important segment, because it could span over several ones
            //  A---|----|-*-|---|---B
            // * we look here, A and B the ends of the segment, from * perspective, AB is non existent

            this.longitudeCount = (int)Math.Floor(lon_len / seg_len);
            this.latitudeCount = (int)Math.Floor(lat_len / seg_len);

            this.longitudeTileLength = lon_len / this.longitudeCount;
            this.latitudeTileLength = lat_len / this.latitudeCount;

            var buckets = Enumerable.Range(0, longitudeCount * latitudeCount).Select(_ => new List<T>()).ToList();

            foreach (T seg in segments)
            {
                int a_idx = getTileIndex(seg.A);
                int b_idx = getTileIndex(seg.B);
                buckets[a_idx].Add(seg);
                if (a_idx != b_idx)
                    buckets[b_idx].Add(seg);
            }

           // Console.WriteLine($"Median occupancy {buckets.OrderBy(it => it.Count).ToList()[buckets.Count/2].Count}");

//            this.tiles = buckets.Select(it => new SortedTile<T>(it)).ToList();
            this.tiles = buckets.Select(it => new PlainTile<T>(it)).ToList();
        }

        private static void computeBoundaries(IEnumerable<T> segments, out Length maxSegmentLength,
            out Angle most_west, out Angle most_east, out Angle most_north, out Angle most_south)
        {
            maxSegmentLength = Length.Zero;
            most_west = Angle.FromDegrees(361);
            most_east = Angle.FromDegrees(-1);
            most_north = Angle.FromDegrees(-91);
            most_south = Angle.FromDegrees(+91);

            foreach (T seg in segments)
            {
                maxSegmentLength = maxSegmentLength.Max(seg.A.GetDistance(seg.B));

                most_west = most_west.Min(seg.A.Longitude);
                most_west = most_west.Min(seg.B.Longitude);
                most_east = most_east.Max(seg.A.Longitude);
                most_east = most_east.Max(seg.B.Longitude);
                most_south = most_south.Min(seg.A.Latitude);
                most_south = most_south.Min(seg.B.Latitude);
                most_north = most_north.Max(seg.A.Latitude);
                most_north = most_north.Max(seg.B.Latitude);
            }
        }

        private int getTileIndex(IGeoPoint point)
        {
            if (!tryGetTileIndices(point, out int lonIndex, out int latIndex))
                throw new ArgumentOutOfRangeException();
            return combineIndices(lonIndex, latIndex);
        }

        private int combineIndices(int lonIndex, int latIndex)
        {
            return latIndex * this.longitudeCount + lonIndex;
        }

        private bool tryGetTileIndices(IGeoPoint point, out int lonIndex, out int latIndex)
        {
            lonIndex = (int)Math.Floor((this.longitudeCount - 1) * (point.Longitude - mostWest) / (mostEast - mostWest));
            latIndex = (int)Math.Floor((this.latitudeCount - 1) * (point.Latitude - mostSouth) / (mostNorth - mostSouth));

            if (lonIndex < 0 || lonIndex >= this.longitudeCount || latIndex < 0 || latIndex >= this.latitudeCount)
            {
                return false;
            }

            return true;
        }

        public bool FindCloseEnough<P>(P point, Length limit, out T nearby, out Length distance) where P : IGeoPoint
        {
            distance = Length.MaxValue;
            nearby = default(T);

            bool result = false;
            foreach (ITile<T> tile in getTiles(point, limit))
                if (tile.FindCloseEnough(point, limit, ref nearby, ref distance))
                    result = true;

            return result;
        }

        public bool IsWithinLimit<P>(P point, Length limit, out Length distance) where P : IGeoPoint
        {
            foreach (ITile<T> tile in getTiles(point, limit))
                if (tile.IsWithinLimit(point, limit, out distance))
                    return true;

            distance = Length.Zero;
            return false;
        }

        private IEnumerable<ITile<T>> getTiles<P>(P point, Length limit)
            where P : IGeoPoint
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
                        indices.Add(Tuple.Create(lon,lat));
                }
            }

            // starting from the center of the region
            return indices
                .OrderBy(it => (it.Item1-lonIndex) * (it.Item1-lonIndex) + (it.Item2-latIndex) * (it.Item2-latIndex))
                .Select(it => this.tiles[combineIndices(it.Item1, it.Item2)]);
        }
    }
}