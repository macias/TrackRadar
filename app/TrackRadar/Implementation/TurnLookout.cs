﻿using Geo;
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

        private static readonly (Length, int) crossroadInitDistance = (Length.MaxValue, -1);


        private readonly IRadarService service;
        private readonly IAlarmSequencer alarmSequencer;
        private readonly ITimeStamper timeStamper;
        private readonly IGeoMap trackMap;
        private readonly IPlanData planData;
        private readonly List<int> crossroadAlarmCount;
        private readonly int[] crossroadLeaveCount;
        private readonly (Length distance, int sectionId)[] crossroadDistances;
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

        internal bool AlarmTurnAhead(in GeoPoint currentPoint,
            ISegment segment, in ArcSegmentIntersection crosspointInfo,
            Speed currentSpeed, long now, out string reason)
        {
            if (this.planData.Graph == null)
            {
                reason = "Navigation disabled";
                return false;
            }

            if (segment == null)
            {
                reason = "Not enough data";
                return false;
            }

            bool result;
            try
            {
                result = doAlarmTurnAhead(currentPoint, segment, crosspointInfo, currentSpeed, now, out reason);
            }
#pragma warning disable CS0168 // do not warn about `ex`
            catch (Exception ex)
#pragma warning restore 
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

        private bool doAlarmTurnAhead(in GeoPoint currentPoint,
            ISegment segment, in ArcSegmentIntersection crosspointInfo,
            Speed currentSpeed, long now, out string reason)
        {

            reason = "No reason given";

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

            int cx_index;

            if (this.planData.Graph.TryGetClosestCrossroad(segment, crosspointInfo,
                out TurnPointInfo primary_cx, out TurnPointInfo? alternate_cx))
                cx_index = crossroadIndexOf(primary_cx.TurnPoint);
            else
            {
                cx_index = -1;
            }

            (GeoPoint closest_cx, Length cx_dist) = primary_cx;

            // todo: maybe zero it sooner -- when it is detected we are already passed crossroad (i.e. moving away)
            if (currentSpeed != Speed.Zero)
            {
                if (cx_index == -1 || cx_dist > GetTurnClearDistance(turn_ahead_distance))
                {
                    for (int i = 0; i < this.planData.Crossroads.Count; ++i)
                    {
                        this.crossroadDistances[i] = crossroadInitDistance;
                        this.crossroadLeaveCount[i] = 0;
                        this.crossroadAlarmCount[i] = 0;
                    }
                }
            }

            bool primary_keep_running = true;
            bool primary_keep_on_warn_limit = true;

            if (cx_index != -1)
                handleDistanceRecord(segment, cx_index, cx_dist);

            if (currentSpeed == Speed.Zero) // if we are walking do not do any additional processing
                return false;

            if (this.planData.Graph.IsApproxTurnNode(crosspointInfo.Intersection))// rare, but in such case it is better not to give some crazy alarms
            {
                reason = $"Right on spot, hard to tell the turn direction";
                alarmSequencer.NotifyAlarm(Alarm.Crossroad); // not sure if this is needed
                return false;
            }

            if (cx_dist > turn_ahead_distance)
                cx_index = -1;

            if (cx_index != -1)
            {
                primary_keep_running = keepOnLeave(cx_index, out reason);

                if (primary_keep_running)
                {
                    if (!keepOnWarnLimit(cx_index, out reason))
                    {
                        alarmSequencer.NotifyAlarm(Alarm.Crossroad);

                        primary_keep_on_warn_limit = false;
                        primary_keep_running = false;
                    }
                }
            }

            this.lastPrimaryCrossroadIndex = cx_index;

            if (// we are hitting the same crossroad as before
                lastPrimaryCrossroadIndex != -1 && cx_index == lastPrimaryCrossroadIndex
                // we are leaving turn node
                && this.crossroadLeaveCount[cx_index] > 0
                && alternate_cx.HasValue)
            {
                (closest_cx, cx_dist) = alternate_cx.Value;

                if (cx_dist <= turn_ahead_distance)
                {
                    cx_index = crossroadIndexOf(closest_cx);
                }
                else
                {
                    cx_index = -1;
                }
            }
            else if (!primary_keep_running)
            {
                if (!primary_keep_on_warn_limit) // we reached the limit of alarms for this turn
                {
                    // we quit anyway, but let's check if we have adjacent (double) turn ahead
                    return adjacentTurnAlarm(currentSpeed, closest_cx, alternate_cx?.TurnPoint, cx_dist, ref reason);
                }


                return false;
            }

            if (cx_dist > turn_ahead_distance)
            {
                reason = $"Too far {cx_dist} to turn ahead alarm distance {turn_ahead_distance}";
                return false;
            }

            bool primary_kept = cx_index == lastPrimaryCrossroadIndex;

            if (!primary_kept)
            {
                handleDistanceRecord(segment, cx_index, cx_dist);

                if (!keepOnWarnLimit(cx_index, out reason))
                {
                    alarmSequencer.NotifyAlarm(Alarm.Crossroad);
                    return false;
                }
            }

            TurnKind? turn_kind = null;

            // if we have tight turns, when we leave one turn and already the second one is in proximity of the alarm
            // then drop the introductory, generic, alarm and go straight to informative alarm (right-cross, left-easy, etc)
            int proper_alarm_after = 1;
            if (!primary_kept) // when switched to alt-turn lower the limit
                proper_alarm_after = 0;
            else
            {
                // rider can accelerate, how much it is hard to say, so we add some slack by adding "1" to the number
                // of alarms needed
                if (cx_dist < alarmsNeededDistance(currentSpeed, crossroadWarningLimit + 1))
                    proper_alarm_after = 0;
            }

            if (this.crossroadAlarmCount[cx_index] >= proper_alarm_after
                && this.planData.Graph.TryGetOutgoingArmSection(closest_cx, segment.SectionId, out ArmSectionPoints outgoing_arm_points))
            {
                // in theory we could compute the next turn, but we have to make check if are moving towards turn point
                // in predictable way, example:
                // |
                // |
                // *----
                // the above is a track
                // and our movement is
                // ^
                // |
                // because we are getting back on track from off-track point (shop, restroom)
                // so it is better not to give "go-ahead" or "turn-right" info, because this could mislead

                // when we hit exactly turn node it is clear we cannot tell which way to go, because all ways
                // are equally good (well, there is place of improvement, we could exclude the one we came here)
                bool skip_turn_info;
                {
                    // it would be great to drop those computings and use current bearing and segment bearing (those are cheap to get)
                    // but the problem is in turns -- if we compute current bearing as (some past point, current point)
                    // then in turns the bearing will be wrong 
                    // if we use (point a second ago, current point) this is regular cost to compute and I am afraid
                    // it will not be stable enough

                    Angle intersection_cx_bearing = GeoCalculator.GetBearing(crosspointInfo.Intersection, closest_cx);
                    Angle point_cx_bearing = GeoCalculator.GetBearing(currentPoint, closest_cx);

                    Angle bearing_diff = GeoCalculator.AbsoluteBearingDifference(intersection_cx_bearing, point_cx_bearing);
                    if (bearing_diff > Angle.PI) // do NOT use modulo, this has to be mirror symmetry
                        bearing_diff = Angle.PI * 2 - bearing_diff;

                    skip_turn_info = bearing_diff.Degrees > 50;
                }

                if (skip_turn_info)
                {
                    ;// this part prevents computing turn info in order not to give false directions

                    if (this.crossroadAlarmCount[cx_index] > 0) // we already warned user, so better not waste alarm for another one
                    {
                        reason = $"Current bearing not aligned with intersection bearing";
                        alarmSequencer.NotifyAlarm(Alarm.Crossroad); // not sure if this is needed
                        return false;
                    }
                }
                else
                {
                    Turn turn = this.planData.Graph.ComputeTurn(currentPoint, closest_cx, cx_dist, outgoing_arm_points);

                    turn_kind = getTurnKind(turn.BearingA, turn.BearingB + Angle.PI);
                }
            }

            return playAlarm(closest_cx, cx_dist, cx_index, turn_kind.HasValue ? turn_kind.Value.ToAlarm() : Alarm.Crossroad, out reason);
        }

        private Length alarmsNeededDistance(Speed currentSpeed, int count)
        {
            return currentSpeed * (service.TurnAheadAlarmInterval + alarmSequencer.MaxTurnDuration) * count;
        }

        private bool adjacentTurnAlarm(Speed currentSpeed, GeoPoint closest_cx, GeoPoint? altTurnPoint, Length cx_dist, ref string reason)
        {
            Length double_turn_advance_distance = currentSpeed * service.DoubleTurnAlarmDistance;
            if (cx_dist <= double_turn_advance_distance)
            {
                IEnumerable<TurnPointInfo> adjacent = this.planData.Graph.GetAdjacentTurns(closest_cx)
                    // skip alternate turn because this turn we already passed (it is "behind our back")
                    .Where(it => altTurnPoint != it.TurnPoint);

                // if we have more than one outgoing track, skip this alarm, user already knows she/he has to slow down and decide
                if (adjacent.Count() == 1)
                {
                    Length double_turn_limit = GetDoubleTurnLengthLimit(currentSpeed);

                    TurnPointInfo closest_next = adjacent.First();

                    // we have to add distance from current point, not only between turn points
                    closest_next = new TurnPointInfo(closest_next.TurnPoint, closest_next.Distance + cx_dist);

                    if (closest_next.Distance < double_turn_limit)
                    {
                        int cx_index = crossroadIndexOf(closest_next.TurnPoint);
                        if (keepOnWarnLimit(cx_index, out reason, limit: 1)) // pre-alarm can be played only once
                        {
                            return playAlarm(closest_next.TurnPoint, closest_next.Distance, cx_index, Alarm.DoubleTurn, out reason);
                        }
                        else
                        {
                            alarmSequencer.NotifyAlarm(Alarm.DoubleTurn);
                            return false;
                        }
                    }

                }
            }

            return false;
        }

        public Length GetDoubleTurnLengthLimit(Speed currentSpeed)
        {
            // do not add "1" because if we launch pre-alarm then we skip initial crossroad alarm after incoming turn
            return alarmsNeededDistance(currentSpeed, crossroadWarningLimit);
        }

        private bool playAlarm(GeoPoint closest_cx, Length cx_dist, int cx_index, Alarm alarm, out string reason)
        {
            service.LogDebug(LogLevel.Info, $"Turn at {closest_cx}, dist {cx_dist}, repeat {this.crossroadAlarmCount[cx_index]}");

            bool played;
            string play_reason;

            played = alarmSequencer.TryAlarm(alarm, out play_reason);

            if (played)
            {
                ++this.crossroadAlarmCount[cx_index];
            }
            else
                service.LogDebug(LogLevel.Warning, $"Turn ahead alarm, couldn't play, reason {play_reason}");

            reason = null;
            return played;
        }

        public static Length GetTurnClearDistance(Length turnAheadDistance)
        {
            return 2 * turnAheadDistance;
        }

        private bool keepOnWarnLimit(int cxIndex, out string reason, int limit = crossroadWarningLimit)
        {
            if (this.crossroadAlarmCount[cxIndex] >= limit)
            {
                reason = $"We already reach the limit {limit} for turn ahead {cxIndex} alarm";
                return false;
            }


            reason = null;
            return true;
        }


        private bool keepOnLeave(int cx_index, out string reason)
        {
            if (this.crossroadAlarmCount[cx_index] > 0)
            {
                // in real world depending solely on distances might be too shaky, because slight variation/error in position
                // and the app will treat it as "leaving", but we will see, avoiding computing means longer battery life so...

                /*
                 Angle current_bearing = GeoCalculator.GetBearing(somePreviousPoint, currentPoint);
                Angle curr_bearing_to_turn_point = GeoCalculator.GetBearing(currentPoint, closest_cx);
                // completely arbitrary limit of angle, it means 22.5 degrees at each side to qualify as movement towards turning point
                bool leaving_by_bearing = GeoCalculator.AbsoluteBearingDifference(current_bearing, curr_bearing_to_turn_point) >= Angle.PI / 4;
            */
                bool leaving_by_dist = this.crossroadLeaveCount[cx_index] > 0;
                if (leaving_by_dist)
                //if (leaving_by_dist)
                {
                    // it is not alarm per se, but it is better to "burn" the turning point we are leaving to save
                    // on further re-checking
                    reason = LeavingTurningPoint;

                    if (this.crossroadAlarmCount[cx_index] < crossroadWarningLimit)
                    {
                        ++this.crossroadAlarmCount[cx_index];
                        alarmSequencer.PostMessage(reason);
                    }

                    return false;
                }
            }




            reason = null;
            return true;
        }

        private void handleDistanceRecord(ISegment segment, int cxIndex, Length cxDistance)
        {
            if (cxDistance <= crossroadDistances[cxIndex].distance
              // if the section id changed it means we are closer to the turning point but we sit on the other arm
              // so we already passed the turning point
              && (crossroadDistances[cxIndex].sectionId == -1 || segment.SectionId == crossroadDistances[cxIndex].sectionId))
            {
                crossroadDistances[cxIndex] = (cxDistance, segment.SectionId);
                crossroadLeaveCount[cxIndex] = 0;
            }
            else
                ++crossroadLeaveCount[cxIndex];
        }

        private int crossroadIndexOf(in GeoPoint cx)
        {
            if (this.planData.Crossroads.TryGetValue(cx, out int index))
                return index;

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
            degrees = MathUnit.Mather.Mod(degrees, 360);

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