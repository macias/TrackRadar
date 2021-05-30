using System;

namespace TrackRadar.Implementation
{
    public partial class GpxLoader
    {
        internal sealed class TracksIndex
        {
            public int IndexOfTrack1 { get; }
            public int IndexOfTrack2 { get; }

            public TracksIndex(int first, int second)
            {
                IndexOfTrack1 = Math.Min(first, second);
                IndexOfTrack2 = Math.Max(first, second);
            }

            public override bool Equals(object obj)
            {
                if (obj is TracksIndex idx)
                    return Equals(idx);
                else
                    return false;
            }

            public bool Equals(TracksIndex other)
            {
                if (ReferenceEquals(other, null))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                if (this.GetType() != other.GetType())
                    return false;

                return this.IndexOfTrack1 == other.IndexOfTrack1 && this.IndexOfTrack2 == other.IndexOfTrack2;
            }

            public override int GetHashCode()
            {
                return IndexOfTrack1.GetHashCode() ^ IndexOfTrack2.GetHashCode();
            }

            public override string ToString()
            {
                return $"[{IndexOfTrack1}:{IndexOfTrack2}]";
            }
        }      
    }
}
