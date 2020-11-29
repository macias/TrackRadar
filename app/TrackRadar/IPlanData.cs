using System.Collections.Generic;
using Geo;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public enum WayPointKind
    {
        Regular, // alarms when leaving, when moving towards (with direction)
        Endpoint // alarm only when moving towards and without (!) direction
    }
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