using MathUnit;
using System;

namespace Geo.Implementation
{
    public static class MeasuredPinnedSegment
    {
        public static IMeasuredPinnedSegment Create(in GeoPoint pin, ISegment segment, Length distance)
        {
            return new MeasuredPinnedSegmentImpl(pin, segment, distance);
        }

        private sealed class MeasuredPinnedSegmentImpl : IMeasuredPinnedSegment
        {
            public GeoPoint Pin { get; }
            public ISegment Segment { get; }
            public Length PinDistance { get; }

            internal MeasuredPinnedSegmentImpl(in GeoPoint pin, ISegment segment, Length distance)
            {
                Pin = pin;
                Segment = segment;
                PinDistance = distance;
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

          /*  public bool Equals(MeasuredPinnedSegmentImpl<P,T> obj)
            {
                if (Object.ReferenceEquals(obj, null))
                    return false;
                if (Object.ReferenceEquals(obj, this))
                    return true;

                if (this.GetType() != obj.GetType())
                    throw new ArgumentException();

                return Object.Equals(this.Pin, obj.Pin) && Object.Equals(this.Segment, obj.Segment);
            }*/

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return $"{this.Pin} @ {this.Segment}";
            }
        }
    }
}