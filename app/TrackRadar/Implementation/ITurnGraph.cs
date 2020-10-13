using Geo;
using MathUnit;
using System.Collections.Generic;

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
    public interface ITurnGraph
    {
#if DEBUG
        IEnumerable<DEBUG_TrackToTurnHack> DEBUG_TrackToTurns { get; }

        bool DEBUG_TrackpointExists(in GeoPoint pt);
        bool DEBUG_TryGetTurnInfo(in GeoPoint trackPoint, out TurnPointInfo primary, out TurnPointInfo? alternate);
#endif

        bool TryGetClosestCrossroad(GeoPoint currentPoint,
            ISegment segment, in GeoPoint projectedPoint,
            Length turnAheadDistance, out GeoPoint crossroad, out Length distance);
        bool TryGetOutgoingArmSection(GeoPoint currentPoint, GeoPoint turnPoint, int sectionId, 
            out ArmSectionPoints sectionPoints);
        Turn ComputeTurn(GeoPoint currentPoint, GeoPoint turnPoint, Length distance, in ArmSectionPoints sectionPoints);
    }
}