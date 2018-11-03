using Gpx;
using System;

namespace Geo
{
    public sealed class Segment : ISegment
    {
        public IGeoPoint A { get; }
        public IGeoPoint B { get; }

        public Segment(IGeoPoint a, IGeoPoint b)
        {
            A = a ?? throw new ArgumentNullException(nameof(a));
            B = b ?? throw new ArgumentNullException(nameof(b));
        }

        public bool IsMoreImportant(ISegment other)
        {
            return false;
        }

    }
}