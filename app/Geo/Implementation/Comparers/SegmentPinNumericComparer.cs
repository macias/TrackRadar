using Geo.Comparers;
using System.Collections.Generic;

namespace Geo.Implementation.Comparers
{
    // the pin (crossing point of the segment) is calculated, so we have to compare it numerically
    // but the segment comes from the map, so we use reference comparison for it
    internal sealed class SegmentPinNumericComparer : IEqualityComparer<IPinnedSegment>
    {
        public static IEqualityComparer<IPinnedSegment> Default { get; } = new SegmentPinNumericComparer();

        private SegmentPinNumericComparer()
        {

        }

        public bool Equals(IPinnedSegment x, IPinnedSegment y)
        {
            return  x.Pin.Equals(y.Pin) 
                && ReferenceComparer<ISegment>.Default.Equals(x.Segment, y.Segment);
        }

        public int GetHashCode(IPinnedSegment obj)
        {
            return ReferenceComparer<ISegment>.Default.GetHashCode(obj.Segment) 
                ^ obj.Pin.GetHashCode();
        }


    }
}