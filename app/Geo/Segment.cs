using System;

namespace Geo
{
    // todo: use tracknode as segment
    /*public sealed class Segment : ISegment
    {
        public int SectionId { get; }
        public GeoPoint A { get; }
        public GeoPoint B { get; }

        public Segment(int sectionId, in GeoPoint a, in GeoPoint b)
        {
            SectionId = sectionId;
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

    }*/
}