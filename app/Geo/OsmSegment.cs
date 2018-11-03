using Gpx;
using System;
using System.Collections.Generic;

namespace Geo
{
    public sealed class OsmSegment : ISegment
    {
        public static IEnumerable<OsmSegment> CreateMany(WayKind kind, IReadOnlyList<IGeoPoint> points)
        {
            for (int i = 1; i < points.Count; ++i)
                yield return new OsmSegment(kind, points[i - 1], points[i]);
        }

        public IGeoPoint A { get; }
        public IGeoPoint B { get; }
        public WayKind Kind { get; }

        public OsmSegment(WayKind kind, IGeoPoint a,IGeoPoint b)
        {
            this.Kind = kind ?? throw new System.ArgumentNullException(nameof(kind));
            A = a ?? throw new ArgumentNullException(nameof(a));
            B = b ?? throw new ArgumentNullException(nameof(b));
        }

        public bool IsMoreImportant(ISegment other)
        {
            return this.Kind.IsMoreImportant(((OsmSegment)other).Kind);
        }

    }
}