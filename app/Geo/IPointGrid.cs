using System.Collections.Generic;
using MathUnit;

namespace Geo
{
    public interface IPointGrid
    {
        IEnumerable<GeoPoint> GetNearby(in GeoPoint point, Length limit);
    }
}