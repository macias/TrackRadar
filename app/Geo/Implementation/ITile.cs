using System.Collections.Generic;
using MathUnit;

namespace Geo.Implementation
{
    internal interface ITile
    {
        IEnumerable<ISegment> Segments { get; }

        bool FindCloseEnough(in GeoPoint point, Length limit, ref ISegment nearby, ref Length? distance, out ArcSegmentIntersection crosspointInfo);
        bool IsWithinLimit(in GeoPoint point, Length limit, out Length? distance);
        IEnumerable<IMeasuredPinnedSegment> FindAll( GeoPoint point, Length limit);
    }

    public static class TileExtension
    {
        internal static bool FindClosest(this ITile @this, in GeoPoint point, ref ISegment nearby, ref Length? distance, 
            out ArcSegmentIntersection crosspointInfo)
        {
            return @this.FindCloseEnough(point, limit: Length.Zero, ref nearby, ref distance, out crosspointInfo);
        }

    }
}