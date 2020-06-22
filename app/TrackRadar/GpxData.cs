using Geo;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar
{
    public sealed class GpxData
    {
        public IEnumerable<ISegment> Segments { get; }
        public IReadOnlyList<GeoPoint> Crossroads { get; }
        //public IReadOnlyDictionary<GeoPoint, Turn> TurnInfo { get; }

        public GpxData(IEnumerable<ISegment> segments,
            IEnumerable<GeoPoint> crossroads
            //,            IReadOnlyDictionary<GeoPoint, Turn> turnInfo
            )
        {
            Segments = segments.ToArray();
            Crossroads = crossroads.ToArray();
          //  TurnInfo = turnInfo;
        }
        /*public GpxData(IEnumerable<Segment> segments, IEnumerable<IGpxPoint> crossroads)
            : this(segments, crossroads.Select(it =>  GeoPoint.FromGpx(it)))
        {
        }*/
    }
}
