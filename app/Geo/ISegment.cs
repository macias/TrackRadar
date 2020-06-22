﻿using System.Collections.Generic;

namespace Geo
{
    public interface ISegment
    {
        GeoPoint A { get; }
        GeoPoint B { get; }

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