using System.Collections.Generic;
using MathUnit;

namespace Geo.Implementation
{
    internal interface ITile
    {
        IEnumerable<ISegment> Segments { get; }

        bool FindCloseEnough(in GeoPoint point, Length limit, ref ISegment nearby, ref Length? distance);
        bool FindClosest(in GeoPoint point, ref ISegment nearby, ref Length? distance);
        bool IsWithinLimit(in GeoPoint point, Length limit, out Length? distance);
        IEnumerable<IMeasuredPinnedSegment> FindAll( GeoPoint point, Length limit);
    }
}