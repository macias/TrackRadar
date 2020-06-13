
namespace Geo
{
    public interface IPinnedSegment
    {
        GeoPoint Pin { get; }
        ISegment Segment { get; }
    }
}