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
        private readonly HashSet<GeoPoint> approxTurnNodes;
        private readonly IReadOnlyDictionary<GeoPoint, (TurnArm a, TurnArm b)> turnsToArms;
        private readonly IReadOnlyDictionary<GeoPoint, IEnumerable<TurnPointInfo>> turnPointsGraph;

#if DEBUG
        public IEnumerable<DEBUG_TrackToTurnHack> DEBUG_TrackToTurns
            => this.trackToTurns.Select(it => new DEBUG_TrackToTurnHack(it.Key, it.Value.primary, it.Value.alternate));
#endif
        public TurnGraph(IEnumerable<GeoPoint> turnNodes,
            IReadOnlyDictionary<GeoPoint, TurnPointInfo> trackToTurns,
            IReadOnlyDictionary<GeoPoint, TurnPointInfo> alternateTrackToTurns,
            IReadOnlyDictionary<GeoPoint, (TurnArm a, TurnArm b)> turnsToArms, 
            IReadOnlyDictionary<GeoPoint, IEnumerable<TurnPointInfo>> turnPointsGraph)
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
            this.approxTurnNodes = new HashSet<GeoPoint>(turnNodes, SufficientlySameComparer.Default);
            this.turnsToArms = turnsToArms;
            this.turnPointsGraph = turnPointsGraph;
        }

#if DEBUG
        public bool DEBUG_TrackpointExists(in GeoPoint pt)
        {
            return trackToTurns.ContainsKey(pt);
        }
#endif

        public bool IsApproxTurnNode(GeoPoint point)
        {
            return approxTurnNodes.Contains(point);
        }
        public bool TryGetClosestCrossroad(ISegment segment, in ArcSegmentIntersection crosspointInfo,
            out TurnPointInfo crossroadInfo, out TurnPointInfo? alternate)
        {
            alternate = default;

            // given segment does not have to have any info about turns, so we need to TRY get some info
            if (!trackToTurns.TryGetValue(segment.A, out var a_pinfo_pair))
            {
                crossroadInfo = default;
                return false;
            }

            (TurnPointInfo a_primary, TurnPointInfo? a_alt) = a_pinfo_pair;

            // when one end has turn info, then sure the other end has the info as well
            (TurnPointInfo b_primary, TurnPointInfo? b_alt) = trackToTurns[segment.B];

            Length a_part_dist = crosspointInfo.AlongSegmentDistance;
            Length b_part_dist = crosspointInfo.SegmentLength - crosspointInfo.AlongSegmentDistance;
            Length a_dist = a_primary.Distance + a_part_dist;
            Length b_dist = b_primary.Distance + b_part_dist;
            if (a_dist < b_dist)
            {
                crossroadInfo = new TurnPointInfo(a_primary.TurnPoint, a_dist);
                //if (a_alt.HasValue)
                // the alt turn point is the other way so we have to subtract, not add
                //  alternate = new TurnPointInfo(a_alt.Value.TurnPoint, a_alt.Value.Distance - a_part_dist);
                alternate = computeAlternate(a_primary.TurnPoint, a_alt, a_part_dist, b_primary, b_alt, b_part_dist);
            }
            else
            {
                crossroadInfo = new TurnPointInfo(b_primary.TurnPoint, b_dist);
                //if (b_alt.HasValue)
                //  alternate = new TurnPointInfo(b_alt.Value.TurnPoint, b_alt.Value.Distance - b_part_dist);
                alternate = computeAlternate(b_primary.TurnPoint, b_alt, b_part_dist, a_primary, a_alt, a_part_dist);
            }

            return true;
        }

        private static TurnPointInfo? computeAlternate(in GeoPoint currentPrimaryTurnPoint, in TurnPointInfo? currentAlt,
            Length currentPart,
            in TurnPointInfo otherPrimary, in TurnPointInfo? otherAlt, Length otherPart)
        {

            if (currentAlt.HasValue) // current (closest) node is NOT a turn node
                // the alt turn point is the other way so we have to subtract, not add
                return new TurnPointInfo(currentAlt.Value.TurnPoint, currentAlt.Value.Distance - currentPart);
            // current node is a turn node, so we have to rely on the other one
            // the other one can point in the same direction or not
            else if (currentPrimaryTurnPoint != otherPrimary.TurnPoint)
                return new TurnPointInfo(otherPrimary.TurnPoint, otherPrimary.Distance + otherPart);
            else if (otherAlt.HasValue)
                return new TurnPointInfo(otherAlt.Value.TurnPoint, otherAlt.Value.Distance + otherPart);
            else
                // unusual, but possible: track has simply one turn, so there is primary info, but no alternate
                return null;
        }
        public bool TryGetOutgoingArmSection(GeoPoint turnPoint, int sectionId, out ArmSectionPoints sectionPoints)
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

        public IEnumerable<TurnPointInfo> GetAdjacentTurns(GeoPoint turnPoint)
        {
            return this.turnPointsGraph[turnPoint];
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