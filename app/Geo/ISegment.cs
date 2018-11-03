using Gpx;

namespace Geo
{
    public interface ISegment
    {
        IGeoPoint A { get; }
        IGeoPoint B { get; }

        bool IsMoreImportant(ISegment other);
    }

}