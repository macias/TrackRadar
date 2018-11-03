using Gpx;
using System.Collections.Generic;

namespace Geo
{
    public sealed class Way
    {
        public WayKind Kind { get; }
        public IReadOnlyList<IGeoPoint> Points { get; }

        public Way(WayKind kind, IReadOnlyList<IGeoPoint> points)
        {
            this.Kind = kind ?? throw new System.ArgumentNullException(nameof(kind));
            this.Points = points ?? throw new System.ArgumentNullException(nameof(points));
        }
    }
}