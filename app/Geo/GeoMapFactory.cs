
using Geo.Implementation;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo
{
    public static class GeoMapFactory
    {
        public static Length SegmentTileLimit { get; } = Length.FromKilometers(1.0);
        public static Length PointTileLimit { get; } = Length.FromMeters(100);

        public static IGeoMap CreateMap(IEnumerable<ISegment> segments)
        {
            return new GeoMap(segments);
        }

        public static IGeoMap CreateSegmentGrid(IEnumerable<ISegment> segments)
        {
            return new SegmentGrid(segments);
        }

        public static IPointGrid CreatePointGrid(IEnumerable<GeoPoint> points,Length tileSize)
        {
            return new PointGrid(points, tileSize);
        }

        public static IGeoMap CreateGrid(IEnumerable<ISegment> segments,
            // (probably) input: point with some traits, point with coordinates, output: combined point
            Func<GeoPoint, GeoPoint, GeoPoint> pointFactory,
            // as above
            Func<ISegment, GeoPoint, GeoPoint, ISegment> segmentFactory,
            Length splitLimit)
        {
            return new SegmentGrid(SplitSegments(segments, pointFactory, segmentFactory, splitLimit));
        }

        public static IEnumerable<ISegment> SplitSegments(IEnumerable<ISegment> segments,
            Func<GeoPoint, GeoPoint, GeoPoint> pointFactory,
            Func<ISegment, GeoPoint, GeoPoint, ISegment> segmentFactory,
            Length splitLimit)
        {
            // we need to split long segments because we don't like to have huge tiles/grids
            // of map containing tons of other segments just because one was long, splitting
            // allow us to have decent size tiles of the map
            var result = new List<ISegment>(capacity: segments.Count());

            foreach (ISegment seg in segments)
            {
                splitSegment(result, seg, seg.A, seg.B, GeoCalculator.GetDistance(seg.A, seg.B), pointFactory,
                    segmentFactory, splitLimit, first: true);
            }

            return result;
        }

        private static void splitSegment(List<ISegment> result, ISegment seg, GeoPoint a, GeoPoint b, Length length,
            Func<GeoPoint, GeoPoint, GeoPoint> pointFactory,
            Func<ISegment, GeoPoint, GeoPoint, ISegment> segmentFactory,
            Length splitLimit, bool first)
        {
            if (length <= splitLimit)
            {
                result.Add(first ? seg : segmentFactory(seg, a, b));
            }
            else
            {
                GeoPoint mid = pointFactory(a, GeoCalculator.GetMidPoint(a, b));

                length /= 2;

                splitSegment(result, seg, a, mid, length, pointFactory, segmentFactory, splitLimit, first: false);
                splitSegment(result, seg, mid, b, length, pointFactory, segmentFactory, splitLimit, first: false);
            }
        }

    }
}