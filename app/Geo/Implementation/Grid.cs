using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal abstract class Grid<TInput, TTile>
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

        protected readonly int longitudeCount;
        protected readonly int latitudeCount;
        protected readonly Length minLongitudeTileLength;
        protected readonly Length latitudeTileLength;
        protected readonly Angle westmost, eastmost, northmost, southmost;
        protected readonly IReadOnlyList<TTile> tiles;

        protected Grid(IEnumerable<TInput> input, Func<IEnumerable<TInput>, IEnumerable<GeoPoint>> conv, Length tileSize)
        {
            if (!MapCalculator.TryGetBoundaries(conv(input),
                out westmost, out eastmost, out northmost, out southmost))
            {
                this.tiles = new List<TTile>();
            }
            else
            {
                Length lat_len, min_lon_len;
                {
                    // we are taking the shortest length along longitude
                    Angle lat = southmost.Abs().Max(northmost.Abs()); // at this lat, the distance will be squeezed most
                    min_lon_len = GeoCalculator.GetDistance(new Geo.GeoPoint(longitude: westmost, latitude: lat),
                        new Geo.GeoPoint(longitude: eastmost, latitude: lat));
                }

                // just how are our map is long along latitude (Y), picking longitude does not matter
                // because longitude change does not squeeze distances
                lat_len = GeoCalculator.GetDistance(new Geo.GeoPoint(longitude: westmost, latitude: northmost),
                        new Geo.GeoPoint(longitude: westmost, latitude: southmost));

                this.longitudeCount = Math.Max(1, (int)Math.Floor(min_lon_len / tileSize));
                this.latitudeCount = Math.Max(1, (int)Math.Floor(lat_len / tileSize));

                this.minLongitudeTileLength = min_lon_len / this.longitudeCount;
                this.latitudeTileLength = lat_len / this.latitudeCount;

                List<List<TInput>> buckets = Enumerable.Range(0, longitudeCount * latitudeCount)
                    .Select(_ => new List<TInput>())
                    .ToList();

                this.tiles = tileBuckets(input, buckets);
                // our tiles will look like this, so the more to the north/south the less area they cover
                // this is a problem if our map cover big space, so that the differences are noticeable
                //      -----
                //     /  |  \
                //    ---------
                //   /    |    \
                //  -------------
            }
        }

        protected abstract IReadOnlyList<TTile> tileBuckets(IEnumerable<TInput> input, List<List<TInput>> buckets);

        protected int getTileIndex(in Geo.GeoPoint point)
        {
            if (!tryGetTileIndices(point, out int lonIndex, out int latIndex))
                throw new ArgumentOutOfRangeException();
            return combineIndices(lonIndex, latIndex);
        }

        protected int combineIndices(int lonIndex, int latIndex)
        {
            return latIndex * this.longitudeCount + lonIndex;
        }

        protected bool tryGetTileIndices(in Geo.GeoPoint point, out int lonIndex, out int latIndex)
        {
            if (this.longitudeCount == 0) // we had no points as input
            {
                lonIndex = 0;
                latIndex = 0;
                return false;
            }

            if (this.longitudeCount == 1) // maybe we should check boundaries?
                lonIndex = 0;
            else
            {
                Angle lon_span = eastmost - westmost;
                Angle lon_relative = point.Longitude - westmost;
                lonIndex = (int)Math.Floor((this.longitudeCount - 1) * lon_relative / lon_span);
            }

            if (this.latitudeCount == 1) // as above
                latIndex = 0;
            else
            {
                Angle lat_span = northmost - southmost;
                Angle lat_relative = (point.Latitude - southmost);
                latIndex = (int)Math.Floor((this.latitudeCount - 1) * lat_relative / lat_span);
            }


            if (lonIndex < 0 || lonIndex >= this.longitudeCount || latIndex < 0 || latIndex >= this.latitudeCount)
            {
                return false;
            }

            return true;
        }

        protected IEnumerable<TTile> getTilesCloserThan(in Geo.GeoPoint point, Length upperLimit)
        {
            tryGetTileIndices(point, out int pt_lon_index, out int pt_lat_index);

            int lon_half_count;
            if (this.longitudeCount == 1) // there is only one tile, so there is no point in computing span (and besides calculation would be wrong when tile contains only single point)
                lon_half_count = 1;
            else
                lon_half_count = 1 + (int)Math.Floor(upperLimit / this.minLongitudeTileLength);

            int lat_half_count;
            if (this.latitudeCount == 1) // see remark above
                lat_half_count = 1;
            else
                lat_half_count = 1 + (int)Math.Floor(upperLimit / this.latitudeTileLength);

            var indices = new List<(int lon, int lat)>();
            for (int lon_idx = pt_lon_index - lon_half_count; lon_idx <= pt_lon_index + lon_half_count; ++lon_idx)
            {
                for (int lat_idx = pt_lat_index - lat_half_count; lat_idx <= pt_lat_index + lat_half_count; ++lat_idx)
                {
                    if (lon_idx >= 0 && lat_idx >= 0 && lon_idx < this.longitudeCount && lat_idx < this.latitudeCount)
                        indices.Add((lon_idx, lat_idx));
                }
            }

            // starting from the center of the region
            return indices
                .OrderBy(it => (it.lon - pt_lon_index) * (it.lon - pt_lon_index) + (it.lat - pt_lat_index) * (it.lat - pt_lat_index))
                .Select(it => this.tiles[combineIndices(it.lon, it.lat)]);
        }

    }
}