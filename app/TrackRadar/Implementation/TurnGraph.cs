using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    internal sealed class TurnGraph : ITurnGraph
    {
        // track point -> closest turn point + distance to it
        // since at most each section is between two turning points we add info about the "other" end of it
        private readonly IReadOnlyDictionary<GeoPoint, (TurnPointInfo primary, TurnPointInfo? alternate)> trackToTurns;
        private readonly IReadOnlyDictionary<GeoPoint, (TurnArm a, TurnArm b)> turnsToArms;

#if DEBUG
        public IEnumerable<DEBUG_TrackToTurnHack> DEBUG_TrackToTurns
            => this.trackToTurns.Select(it => new DEBUG_TrackToTurnHack(it.Key, it.Value.primary, it.Value.alternate));
#endif
        public TurnGraph(IReadOnlyDictionary<GeoPoint, TurnPointInfo> trackToTurns,
            IReadOnlyDictionary<GeoPoint, TurnPointInfo> alternateTrackToTurns,
            IReadOnlyDictionary<GeoPoint, (TurnArm a, TurnArm b)> turnsToArms)
        {
            var track_to_turns = new Dictionary<GeoPoint, (TurnPointInfo primary, TurnPointInfo? alternate)>();
            foreach (var entry in trackToTurns)
            {
                TurnPointInfo? alternate = null;
                if (alternateTrackToTurns.TryGetValue(entry.Key, out TurnPointInfo info))
                    alternate = info;
                track_to_turns.Add(entry.Key, (entry.Value, alternate));
            }
            this.trackToTurns = track_to_turns;

            this.turnsToArms = turnsToArms;
        }

#if DEBUG
        public bool DEBUG_TrackpointExists(in GeoPoint pt)
        {
            return trackToTurns.ContainsKey(pt);
        }
#endif

        public bool TryGetClosestCrossroad(GeoPoint currentPoint,
            ISegment segment, in GeoPoint projectedPoint,
            Length turnAheadDistance, out GeoPoint crossroad, out Length distance)
        {
            if (!trackToTurns.TryGetValue(segment.A, out var a_pinfo_pair))
            {
                crossroad = default;
                distance = default;
                return false;
            }

            (TurnPointInfo a_info, TurnPointInfo? _) = a_pinfo_pair;

            TurnPointInfo b_info = trackToTurns[segment.B].primary;

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
            else if (sectionId == arms.b.SectionId)
                sectionPoints = arms.a.SectionPoints;
            else
            {
#if DEBUG
                throw new InvalidOperationException($"Cannot match section id {sectionId} with turn arms {arms.a.SectionId} and {arms.b.SectionId}");
#else
                sectionPoints = default;
                return false;
#endif
            }

            return true;
        }

        public Turn ComputeTurn(GeoPoint currentPoint, GeoPoint turnPoint, Length distance, in ArmSectionPoints sectionPoints)
        {
            GeoPoint other_point = getTrackPoint(sectionPoints.TrackPoints, distance);
            return new Turn(GeoCalculator.GetBearing(currentPoint, turnPoint), GeoCalculator.GetBearing(other_point, turnPoint));
        }

        private GeoPoint getTrackPoint(IEnumerable<(GeoPoint trackPoint, Length distance)> sectionPoints, Length distance)
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
#if DEBUG
        public bool DEBUG_TryGetTurnInfo(in GeoPoint trackPoint, out TurnPointInfo primary, out TurnPointInfo? alternate)
        {
            if (this.trackToTurns.TryGetValue(trackPoint, out var pair))
            {
                (primary, alternate) = pair;
                return true;
            }

            primary = default;
            alternate = default;
            return false;
        }
#endif
    }
}