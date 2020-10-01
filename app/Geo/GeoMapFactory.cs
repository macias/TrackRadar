
using Geo.Implementation;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo
{
    public static class GeoMapFactory
    {
        public static Length SegmentLengthLimit { get; } = Length.FromKilometers(1.0);
        public static Length PointTileLimit { get; } = Length.FromMeters(100);

        /*public static object CreateGraph(IEnumerable<INodePoint> points)
        {
            return new MeasuredGraph(points);
        }*/

       /* public static IGeoMap CreateMap(IEnumerable<ISegment> segments)
        {
            return new GeoMap(segments);
        }*/

        /*public static IGeoMap CreateSegmentGrid(IEnumerable<ISegment> segments)
        {
            return new SegmentGrid(segments);
        }*/

        /*public static IPointGrid CreatePointGrid(IEnumerable<GeoPoint> points,Length tileSize)
        {
            return new PointGrid(points, tileSize);
        }*/

        public static IGeoMap CreateGrid(IEnumerable<ISegment> segments)
        {
            return new SegmentGrid(segments);
        }

    }
}