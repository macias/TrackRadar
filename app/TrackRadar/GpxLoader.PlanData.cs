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
            public IReadOnlyDictionary<GeoPoint,int> Crossroads { get; }
#if DEBUG
            public int DEBUG_ExtensionCount { get; }
#endif
            public ITurnGraph Graph { get; }

            public PlanData(IEnumerable<ISegment> segments, IEnumerable<GeoPoint> crossroads,
                int debugExtensionCount,
                ITurnGraph graph)
            {
                Segments = segments.ToArray();
                Crossroads = crossroads.ZipIndex().ToDictionary(it => it.value, it=> it.index);
#if DEBUG
                DEBUG_ExtensionCount = debugExtensionCount;
#endif
                this.Graph = graph;
            }
        }
    }
}
