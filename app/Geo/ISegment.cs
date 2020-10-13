using MathUnit;
using System.Collections.Generic;

namespace Geo
{
    public interface ISegment
    {
#if DEBUG
        int __DEBUG_id { get; }
#endif
        int SectionId { get; }
        GeoPoint A { get; }
        GeoPoint B { get; }

        Length GetLength();

        Ordering CompareImportance(ISegment other);
    }

    public static class SegmentExtension
    {
        public static IEnumerable<GeoPoint> Points(this ISegment segment)
        {
            yield return segment.A;
            yield return segment.B;
        }

        public static void Deconstruct(this ISegment segment, out GeoPoint a,out GeoPoint b)
        {
            a = segment.A;
            b = segment.B;
        }

    }

}