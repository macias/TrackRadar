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
    }
}