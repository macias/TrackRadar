using Gpx;
using System;

namespace TrackRadar
{
    internal partial class GpxLoader
    {
        private enum CrossroadKind
        {
            Intersection, // true intersection, X
            Extension, // -- * --
            PassingBy, // distant "intersection", > * <
        }

        private sealed class Crossroad
        {
            public IGeoPoint Point { get; internal set; }
            public CrossroadKind Kind { get; internal set; }
            public Tuple<int, int> SourceIndex { get; internal set; }

            public Crossroad()
            {
                this.Kind = CrossroadKind.Intersection;
            }

            public override string ToString()
            {
                return $"{SourceIndex} {Kind}";
            }
        }
    }
}
