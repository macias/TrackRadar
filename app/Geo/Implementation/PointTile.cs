using Geo.Comparers;
using Geo.Implementation.Comparers;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
    internal sealed class PointTile
    {
        private readonly IReadOnlyList<GeoPoint> points;
        public IEnumerable<GeoPoint> Points => this.points;

        public PointTile(IReadOnlyList<GeoPoint> points)
        {
            this.points = points;
        }
    }

 }