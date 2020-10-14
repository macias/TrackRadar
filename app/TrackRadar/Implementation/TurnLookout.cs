using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    internal sealed class TurnLookout
    {
        public static string LeavingTurningPoint { get; } = "We are leaving turning point";

        private const int crossroadWarningLimit = 3;

        private static readonly Length crossroadInitDistance = Length.MaxValue;


        private readonly IRadarService service;
        private readonly IAlarmSequencer alarmSequencer;
        private readonly ITimeStamper timeStamper;
        private readonly IGeoMap trackMap;
        private readonly IPlanData planData;
        private readonly List<int> crossroadAlarmCount;
        private readonly int[] crossroadLeaveCount;
        private readonly Length[] crossroadDistances;
        private int lastPrimaryCrossroadIndex;

        public TurnLookout(IRadarService service, IAlarmSequencer alarmSequencer, ITimeStamper timeStamper,
            IPlanData gpxData, IGeoMap map)
        {
            this.service = service;
            this.alarmSequencer = alarmSequencer;
            this.timeStamper = timeStamper;

            this.trackMap = map;
            this.planData = gpxData;
            this.crossroadAlarmCount = gpxData.Crossroads.Select(_ => 0).ToList();
            this.crossroadLeaveCount = gpxData.Crossroads.Select(_ => 0).ToArray();
            this.lastPrimaryCrossroadIndex = -1;
            this.crossroadDistances = gpxData.Crossroads.Select(_ => crossroadInitDistance).ToArray();
        }

        internal bool AlarmTurnAhead(in GeoPoint somePreviouPoint, in GeoPoint currentPoint,
            ISegment segment, in ArcSegmentIntersection crosspointInfo,
            Speed currentSpeed, long now, out string reason)
        {
            if (this.planData.Graph == null)
            {
                reason = "Navigation disabled";
                return false;
            }

            bool result;
            try
            {
                result = doAlarmTurnAhead(somePreviouPoint, currentPoint,
                    segment, crosspointInfo,
                    currentSpeed, now, out reason);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#else
                reason = "Crash on turn";
                service.LogDebug(LogLevel.Error, $"CRASH with {ex.Message}");
                result = false;
#endif
            }
            if (result)
                service.WriteCrossroad(latitudeDegrees: currentPoint.Latitude.Degrees, longitudeDegrees: currentPoint.Longitude.Degrees);

            return result;
        }

        private bool doAlarmTurnAhead(in GeoPoint somePreviousPoint, in GeoPoint currentPoint,
            ISegment segment, in ArcSegmentIntersection crosspointInfo,
            Speed currentSpeed, long now, out string reason)
        {
            if (service.TurnAheadAlarmDistance == TimeSpan.Zero)
            {
                reason = $"Turn ahead distance is set to zero";
                return false;
            }

            if (!this.alarmSequencer.TryGetLatestTurnAheadAlarmAt(out long last_turn_alarm_at))
            {
                reason = "Cannot alarm about turn ahead because something is playing";
                alarmSequencer.NotifyAlarm(Alarm.Crossroad);
                return false;
            }
            var passed = timeStamper.GetSecondsSpan(now, last_turn_alarm_at);

            if (passed < service.TurnAheadAlarmInterval.TotalSeconds)
            {
                reason = $"Cannot alarm now {now} about turn ahead. Last alarm was {last_turn_alarm_at}, {passed}s ago";
                alarmSequencer.NotifyAlarm(Alarm.Crossroad);
                return false;
            }

            Length turn_ahead_distance = currentSpeed * service.TurnAheadAlarmDistance;

            bool found = this.planData.Graph.TryGetClosestCrossroad(segment, crosspointInfo,
                out TurnPointInfo primary_cx, out TurnPointInfo? alternate_cx);

            (GeoPoint closest_cx, Length min_dist) = primary_cx;

            if (found && min_dist > turn_ahead_distance)
                found = false;
            int cx_index = found ? crossroadIndexOf(closest_cx) : -1;

            // todo: maybe zero it sooner -- when it is detected we are already passed crossroad (i.e. moving away)
            if (cx_index == -1 || min_dist > 2 * turn_ahead_distance)
            {
                for (int i = 0; i < this.planData.Crossroads.Count; ++i)
                {
                    this.crossroadDistances[i] = crossroadInitDistance;
                    this.crossroadLeaveCount[i] = 0;
                    this.crossroadAlarmCount[i] = 0;
                }
            }

            this.lastPrimaryCrossroadIndex = cx_index;

            if (lastPrimaryCrossroadIndex != -1 && cx_index == lastPrimaryCrossroadIndex
                && this.crossroadLeaveCount[cx_index] > 0
                // above conditions mean "we are leaving turn node"
                && alternate_cx.HasValue)
            {
                (closest_cx, min_dist) = alternate_cx.Value;

                if (found && min_dist > turn_ahead_distance)
                    found = false;

                cx_index = found ? crossroadIndexOf(closest_cx) : -1;
            }

            if (min_dist > turn_ahead_distance)
            {
                reason = $"Too far {min_dist} to turn ahead alarm distance {turn_ahead_distance}";
                return false;
            }

            bool primary_kept = cx_index == lastPrimaryCrossroadIndex;

            if (min_dist > crossroadDistances[cx_index])
                ++crossroadLeaveCount[cx_index];
            else
                crossroadDistances[cx_index] = min_dist;

            if (this.crossroadAlarmCount[cx_index] >= crossroadWarningLimit)
            {
                reason = $"We already reach the limit {crossroadWarningLimit} for turn ahead {cx_index} alarm";
                alarmSequencer.NotifyAlarm(Alarm.Crossroad);
                return false;
            }


            if (this.crossroadAlarmCount[cx_index] > 0 && primary_kept)
            {
                // in real world depending solely on distances might be too shaky, because slight variation/error in position
                // and the app will treat it as "leaving", but we will see, avoiding computing means longer battery life so...

                if (false)
                {
                    Angle current_bearing = GeoCalculator.GetBearing(somePreviousPoint, currentPoint);
                    Angle curr_bearing_to_turn_point = GeoCalculator.GetBearing(currentPoint, closest_cx);
                    // completely arbitrary limit of angle, it means 22.5 degrees at each side to qualify as movement towards turning point
                    bool leaving_by_bearing = GeoCalculator.AbsoluteBearingDifference(current_bearing, curr_bearing_to_turn_point) >= Angle.PI / 4;
                }
                bool leaving_by_dist = this.crossroadLeaveCount[cx_index] > 0 && primary_kept;
                if (leaving_by_dist)
                //if (leaving_by_dist)
                {
                    // it is not alarm per se, but it is better to "burn" the turning point we are leaving to save
                    // on further re-checking
                    ++this.crossroadAlarmCount[cx_index];

                    reason = LeavingTurningPoint;
                    alarmSequencer.PostMessage(reason);
                    return false;
                }
            }

            TurnKind? turn_kind = null;

            // if we have tight turns, when we leave one turn and already the second one is in proximity of the alarm
            // then drop the introductory, generic, alarm and go straight to informative alarm (right-cross, left-easy, etc)
            if (this.crossroadAlarmCount[cx_index] >= (primary_kept ? 1 : 0) // when switched to alt-turn lower the limit
                && this.planData.Graph.TryGetOutgoingArmSection(currentPoint, closest_cx, segment.SectionId,
                    out ArmSectionPoints outgoing_arm_points))
            {
                Turn turn = this.planData.Graph.ComputeTurn(currentPoint, closest_cx, min_dist,
                    outgoing_arm_points);

                turn_kind = getTurnKind(turn.BearingA, turn.BearingB + Angle.PI);
            }



            service.LogDebug(LogLevel.Info, $"Turn at {closest_cx}, dist {min_dist}, repeat {this.crossroadAlarmCount[cx_index]}");

            bool played;
            string play_reason;

            if (turn_kind.HasValue)
            {
                played = alarmSequencer.TryAlarm(turn_kind.Value.ToAlarm(), out play_reason);
            }
            else
                played = alarmSequencer.TryAlarm(Alarm.Crossroad, out play_reason);

            if (played)
            {
                ++this.crossroadAlarmCount[cx_index];
            }
            else
                service.LogDebug(LogLevel.Warning, $"Turn ahead alarm, couldn't play, reason {play_reason}");

            reason = null;
            return played;
        }

        private int crossroadIndexOf(GeoPoint cx)
        {
            for (int i = 0; i < this.planData.Crossroads.Count; ++i)
                if (this.planData.Crossroads[i] == cx)
                    return i;

            return -1;
        }

        internal static bool LegacyComputeTurnKind(Angle currBearing, in Turn turn, out TurnKind turnKind)
        {
            Angle a_diff = GeoCalculator.AbsoluteBearingDifference(currBearing, turn.BearingA);
            Angle b_diff = GeoCalculator.AbsoluteBearingDifference(currBearing, turn.BearingB);

            Angle similar_bearing_limit = Angle.FromDegrees(12);

            if (a_diff < b_diff && a_diff < similar_bearing_limit)
            {
                turnKind = getTurnKind(currBearing, turn.BearingB + Angle.PI); // make it opposite direction (from cx)
                return true;
            }
            else if (b_diff < a_diff && b_diff < similar_bearing_limit)
            {
                turnKind = getTurnKind(currBearing, turn.BearingA + Angle.PI);
                return true;
            }

            turnKind = default;
            return false;
        }

        // we go from segment a to segment b, the order is important here
        // aStart - aTurn - bTurn - bEnd
        internal static TurnKind getTurnKind(in GeoPoint aStart, in GeoPoint aTurn, in GeoPoint bTurn, in GeoPoint bEnd)
        {
            Angle bearing_from = GeoCalculator.GetBearing(aStart, aTurn);
            Angle bearing_to = GeoCalculator.GetBearing(bTurn, bEnd);

            return getTurnKind(bearing_from, bearing_to);
        }

        /// <summary>
        /// the bearing should make a path, so from bearingFrom you turn and continue on bearingTo
        /// </summary>
        internal static TurnKind getTurnKind(Angle bearingFrom, Angle bearingTo)
        {
            // preserve bearing notion, 0 degrees then means we go ahead and counting clockwise
            // 90 degrees we turn to the right, and so on
            double degrees = (bearingTo - bearingFrom).Degrees;
            degrees = Mather.Mod(degrees, 360);

            // since we have turn-zones of 45 degrees, split them in the middle
            const double margin = 45.0 / 2;

            if (degrees < 45 - margin)
                return TurnKind.GoAhead;
            else if (degrees < 90 - margin)
                return TurnKind.RightEasy;
            else if (degrees < 90 + 45 - margin)
                return TurnKind.RightCross;
            else if (degrees < 180)
                return TurnKind.RightSharp;
            else if (degrees < 270 - margin)
                return TurnKind.LeftSharp;
            else if (degrees < 270 + 45 - margin)
                return TurnKind.LeftCross;
            else if (degrees < 360 - margin)
                return TurnKind.LeftEasy;

            return TurnKind.GoAhead;
        }

    }
}