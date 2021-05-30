using Geo;
using MathUnit;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    public interface ITurnGraph
    {
#if DEBUG
        IEnumerable<DEBUG_TrackToTurnHack> DEBUG_TrackToTurnPoints { get; }
        IEnumerable<GeoPoint> DEBUG_TurnPoints { get; }

        bool DEBUG_TrackpointExists(in GeoPoint pt);
        bool DEBUG_TryGetTurnInfo(in GeoPoint trackPoint, out TurnPointInfo primary, out TurnPointInfo? alternate);
#endif

        bool TryGetClosestCrossroad(ISegment segment, in ArcSegmentIntersection crosspointInfo,
            out TurnPointInfo crossroadInfo, out TurnPointInfo? alternate);
        bool TryGetOutgoingArmSection(GeoPoint turnPoint, int sectionId, out ArmSectionPoints sectionPoints);
        Turn ComputeTurn(GeoPoint currentPoint, GeoPoint turnPoint, Length distance, in ArmSectionPoints sectionPoints);
        bool IsApproxTurnNode(GeoPoint point);
        IEnumerable<TurnPointInfo> GetAdjacentTurns(GeoPoint turnPoint);
    }
}