using Gpx;
using System.Collections.Generic;

namespace Geo
{
    public interface IGeoMap<T> where T : ISegment
    {
        IEnumerable<T> Segments { get; }

        bool FindCloseEnough<P>(P point, Length limit, out T nearby, out Length distance)
            where P : IGeoPoint;
        bool IsWithinLimit<P>(P point, Length limit, out Length distance)
            where P : IGeoPoint;
    }
}