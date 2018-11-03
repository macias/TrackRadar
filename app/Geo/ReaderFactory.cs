
using Geo.Implementation;
using System.IO;

namespace Geo
{
    public sealed class ReaderFactory
    {
        public static IReader CreateOsm(Stream stream)
        {
            return new OsmReader(stream);
        }
    }
}