using System.Collections.Generic;
using Geo;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public interface IPlanData
    {
        IReadOnlyList<GeoPoint> Crossroads { get; }
        IEnumerable<ISegment> Segments { get; }
        ITurnGraph Graph { get; }
    }
}