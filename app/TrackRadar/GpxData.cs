using Geo;
using Gpx;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar
{
    public sealed class GpxData
    {
        public IEnumerable<ISegment> Segments { get; }
        public IEnumerable<GeoPoint> Crossroads { get; }

        public GpxData(IEnumerable<ISegment> segments, IEnumerable<GeoPoint> crossroads)
        {
            Segments = segments.ToArray();
            Crossroads = crossroads.ToArray();
        }
        /*public GpxData(IEnumerable<Segment> segments, IEnumerable<IGpxPoint> crossroads)
            : this(segments, crossroads.Select(it =>  GeoPoint.FromGpx(it)))
        {
        }*/
    }
}
