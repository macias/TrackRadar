using System;

namespace Geo
{
    public sealed class Segment : ISegment
    {
        public GeoPoint A { get; }
        public GeoPoint B { get; }

        public Segment(in GeoPoint a, in GeoPoint b)
        {
            A = a;
            B = b;
        }

        public Ordering CompareImportance(ISegment other)
        {
            if (other is Segment seg)
                return Ordering.Equal;
            else
                throw new ArgumentException();
        }

        public override string ToString()
        {
            return $"{A} » {B}";
        }

    }
}