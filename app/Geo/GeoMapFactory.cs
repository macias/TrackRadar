
using Geo.Implementation;
using System.Collections.Generic;

namespace Geo
{
    public sealed class GeoMapFactory
    {
        public static IGeoMap<T> Create<T>(IEnumerable<T> segments)
            where T : ISegment
        {
            return new GeoMap<T>(segments);
        }
    }
}