using Geo;
using MathUnit;
using System;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    public readonly struct ArmSectionPoints
    {
        // the last distance is irrelevant and numericaly can be wrong, because it can be distance to some other turn point
        public IReadOnlyList<(GeoPoint point, Length distance)> TrackPoints { get; }

        public ArmSectionPoints(IReadOnlyList<(GeoPoint point, Length distance)> trackPoints)
        {
            this.TrackPoints = trackPoints;
        }

    }

    public interface ITurnGraph
    {
        bool TryGetClosestCrossroad(GeoPoint currentPoint,
            ISegment segment, in GeoPoint projectedPoint,
            Length turn_ahead_distance, out GeoPoint crossroad, out Length distance);
        bool DEBUG_TrackpointExists(in GeoPoint pt);
        bool TryGetOutgoingArmSection(GeoPoint currentPoint, GeoPoint turnPoint, int sectionId, out ArmSectionPoints sectionPoints);
        Turn ComputeTurn(GeoPoint currentPoint, GeoPoint turnPoint, Length distance, in ArmSectionPoints sectionPoints);
    }

    internal sealed class TurnGraph : ITurnGraph
    {
        // track point -> closest turn point + distance to it
        private readonly IReadOnlyDictionary<GeoPoint, TurnPointInfo> trackToTurns;
        private readonly IReadOnlyDictionary<GeoPoint, (TurnArm a, TurnArm b)> turnsToArms;
        //private readonly IReadOnlyList<>

        public TurnGraph(IReadOnlyDictionary<GeoPoint, TurnPointInfo> trackToTurns,
            IReadOnlyDictionary<GeoPoint, (TurnArm a, TurnArm b)> turnsToArms)
        {
            this.trackToTurns = trackToTurns;
            this.turnsToArms = turnsToArms;
        }

        public bool DEBUG_TrackpointExists(in GeoPoint pt)
        {
            return trackToTurns.ContainsKey(pt);
        }

        public bool TryGetClosestCrossroad(GeoPoint currentPoint,
            ISegment segment, in GeoPoint projectedPoint,
            Length turnAheadDistance, out GeoPoint crossroad, out Length distance)
        {
            if (!trackToTurns.TryGetValue(segment.A, out TurnPointInfo a_info))
            {
                crossroad = default;
                distance = default;
                return false;
            }

            TurnPointInfo b_info = trackToTurns[segment.B];

            Length a_dist = a_info.Distance > turnAheadDistance ? Length.MaxValue : (a_info.Distance + GeoCalculator.GetDistance(currentPoint, segment.A));
            Length b_dist = b_info.Distance > turnAheadDistance ? Length.MaxValue : (b_info.Distance + GeoCalculator.GetDistance(currentPoint, segment.B));
            if (a_dist < b_dist)
            {
                crossroad = a_info.TurnPoint;
                distance = a_dist;
            }
            else
            {
                crossroad = b_info.TurnPoint;
                distance = b_dist;
            }

            bool result = distance <= turnAheadDistance;

            return result;
        }

        public bool TryGetOutgoingArmSection(GeoPoint currentPoint, GeoPoint turnPoint, int sectionId,
            out ArmSectionPoints sectionPoints)
        {
            if (!this.turnsToArms.TryGetValue(turnPoint, out (TurnArm a, TurnArm b) arms))
            {
                sectionPoints = default;
                return false;
            }

            if (sectionId == arms.a.SectionId)
                sectionPoints = arms.b.SectionPoints;
            else
                sectionPoints = arms.a.SectionPoints;

            return true;
        }

        public Turn ComputeTurn(GeoPoint currentPoint, GeoPoint turnPoint, Length distance, in ArmSectionPoints sectionPoints)
        {
            GeoPoint other_point = getTrackPoint(sectionPoints.TrackPoints, distance);
            return new Turn(GeoCalculator.GetBearing(currentPoint, turnPoint), GeoCalculator.GetBearing(other_point, turnPoint));
        }

        private GeoPoint getTrackPoint(IReadOnlyList<(GeoPoint trackPoint, Length distance)> sectionPoints, Length distance)
        {
            GeoPoint result = default;
            foreach (var entry in sectionPoints)
            {
                result = entry.trackPoint;
                if (entry.distance >= distance)
                    break;
            }

            return result;
        }

    }
}