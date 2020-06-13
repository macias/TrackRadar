using MathUnit;

namespace Geo
{
    public interface IMeasuredPinnedSegment  : IPinnedSegment
    {
        // length to pin-point from given (at hand) point
        Length PinDistance { get; }
    }
}