using System.Collections.Generic;
using Gpx;

namespace Geo.Implementation
{
    internal interface ITile<T> where T : ISegment
    {
        IEnumerable<T> Segments { get; }

        bool FindCloseEnough<P>(P point, Length limit, ref T nearby, ref Length distance) where P : IGeoPoint;
        bool IsWithinLimit<P>(P point, Length limit, out Length distance) where P : IGeoPoint;
    }
}