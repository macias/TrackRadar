using Geo;
using MathUnit;

namespace TrackRadar.Implementation
{
    public readonly struct TurnPointInfo
    {
        public GeoPoint TurnPoint { get; }
        public Length Distance { get; }

        public TurnPointInfo(GeoPoint turnPoint,Length distance)
        {
            TurnPoint = turnPoint;
            Distance = distance;
        }

        public void Deconstruct(out GeoPoint turnPoint,out Length distance)
        {
            turnPoint = this.TurnPoint;
            distance = this.Distance;
        }

#if DEBUG
        public override string ToString()
        {
            return $"{Distance} to {TurnPoint}";
        }
#endif
    }
}