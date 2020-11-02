using System.Collections.Generic;
using Geo;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public interface IPlanData
    {
        IReadOnlyDictionary<GeoPoint, int> Crossroads { get; }
        IEnumerable<ISegment> Segments { get; }
        ITurnGraph Graph { get; }
#if DEBUG
        int DEBUG_ExtensionCount { get; }
#endif
    }
}