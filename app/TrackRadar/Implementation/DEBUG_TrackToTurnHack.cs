using Geo;

namespace TrackRadar.Implementation
{
#if DEBUG
    public readonly struct DEBUG_TrackToTurnHack // all of the sudden VS2017 claims it cannot resolve ValueTuple, brilliant
    {
        public GeoPoint TrackPoint { get; }
        public TurnPointInfo Primary { get; }
        public TurnPointInfo? Alternate { get; }

        public DEBUG_TrackToTurnHack(GeoPoint trackPoint, TurnPointInfo primary, TurnPointInfo? alternate)
        {
            TrackPoint = trackPoint;
            Primary = primary;
            Alternate = alternate;
        }

    }
#endif
}