using Geo;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public sealed partial class GpxLoader
    {
        private sealed class PlanData : IPlanData
        {
            public IEnumerable<ISegment> Segments { get; }
            public IReadOnlyList<GeoPoint> Crossroads { get; }
            public ITurnGraph Graph { get; }

            public PlanData(IEnumerable<ISegment> segments, IEnumerable<GeoPoint> crossroads, ITurnGraph graph)
            {
                Segments = segments.ToArray();
                Crossroads = crossroads.ToArray();
                this.Graph = graph;
            }
        }
    }
}
