using Geo;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    internal static class MapHelper
    {    
        internal static IGeoMap CreateDefaultGrid(IEnumerable<ISegment> segments)
        {
            return GeoMapFactory.CreateGrid(segments,
                            (_, p) => p,
                            (_, a, b) => new Segment(a, b),
                            GeoMapFactory.SegmentTileLimit);
        }


    }
}