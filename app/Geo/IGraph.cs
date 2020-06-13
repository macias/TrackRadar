using System.Collections.Generic;

namespace Geo
{
    public interface IGraph
    {
        IEnumerable<GeoPoint> GetAdjacent(in GeoPoint node);
    }
}