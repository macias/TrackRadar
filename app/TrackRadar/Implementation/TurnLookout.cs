using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    internal sealed class TurnLookout
    {
        private const int crossroadsWarningLimit = 3;

        private readonly IRadarService service;
        private readonly IAlarmSequencer alarmSequencer;
        private readonly ITimeStamper timeStamper;
        private readonly IGeoMap trackMap;
        private readonly GpxData gpxData;
        private readonly List<int> crossroadAlarmCount;
        private readonly IPointGrid pointGrid;
        private (int cxIndex, Turn turn) lastTurnInfo;

        public TurnLookout(IRadarService service, IAlarmSequencer alarmSequencer, ITimeStamper timeStamper, GpxData gpxData, IGeoMap map)
        {
            this.service = service;
            this.alarmSequencer = alarmSequencer;
            this.timeStamper = timeStamper;

            this.trackMap = map;
            this.gpxData = gpxData;
            this.crossroadAlarmCount = gpxData.Crossroads.Select(_ => 0).ToList();
            this.pointGrid = GeoMapFactory.CreatePointGrid(gpxData.Crossroads, GeoMapFactory.PointTileLimit);
            this.lastTurnInfo = (cxIndex: -1, turn: default);
        }

        internal
            //AlarmState
            bool AlarmTurnAhead(in GeoPoint somePreviouPoint, in GeoPoint currentPoint, Speed currentSpeed, long now, out string reason)
        {
            var result = doAlarmTurnAhead(somePreviouPoint, currentPoint, currentSpeed, now, out reason);
            if (result)// == AlarmState.Played)
                service.WriteCrossroad(latitudeDegrees: currentPoint.Latitude.Degrees, longitudeDegrees: currentPoint.Longitude.Degrees);

            return result;
        }

        private
            //AlarmState 
            bool doAlarmTurnAhead(in GeoPoint somePreviousPoint, in GeoPoint currentPoint, Speed currentSpeed, long now, out string reason)
        {
            if (service.TurnAheadAlarmDistance == TimeSpan.Zero)
            {
                reason = $"Turn ahead distance is set to zero";
                // return AlarmState.None;
                return false;
            }

            if (!this.alarmSequencer.TryGetLatestTurnAheadAlarmAt(out long last_turn_alarm_at))
            {
                reason = "Cannot alarm about turn ahead because something is playing";
                alarmSequencer.NotifyAlarm(Alarm.Crossroad);
                //                return AlarmState.Postponed; 
                return false;
            }
            var passed = timeStamper.GetSecondsSpan(now, last_turn_alarm_at);

            if (passed < service.TurnAheadAlarmInterval.TotalSeconds)
            {
                reason = $"Cannot alarm now {now} about turn ahead. Last alarm was {last_turn_alarm_at}, {passed}s ago";
                alarmSequencer.NotifyAlarm(Alarm.Crossroad);
                //                return AlarmState.Postponed;
                return false;
            }

            Length turn_ahead_distance = currentSpeed * service.TurnAheadAlarmDistance;

            Length min_dist = Length.MaxValue;
            GeoPoint closest_cx = GeoPoint.FromDegrees(360, 360);

            IEnumerable<GeoPoint> nearby = this.pointGrid.GetNearby(currentPoint, turn_ahead_distance);
            foreach (GeoPoint cx in nearby)
            {
                Length dist = GeoCalculator.GetDistance(cx, currentPoint);
                if (dist < min_dist)
                {
                    min_dist = dist;
                    closest_cx = cx;
                }
            }

            int cx_index = -1;

            for (int i = 0; i < this.gpxData.Crossroads.Count; ++i)
            {
                bool match = this.gpxData.Crossroads[i] == closest_cx;
                if (match)
                    cx_index = i;
                // todo: zero it if it is detected we are already passed crossroad (i.e. moving away)
                if (!match || min_dist > 2 * turn_ahead_distance)
                    this.crossroadAlarmCount[i] = 0;
            }

            if (min_dist > turn_ahead_distance)
            {
                reason = $"Too far {min_dist} to turn ahead alarm distance {turn_ahead_distance}";
                //   return AlarmState.None;
                return false;
            }
            if (this.crossroadAlarmCount[cx_index] >= crossroadsWarningLimit)
            {
                reason = $"We already reach the limit {crossroadsWarningLimit} for turn ahead {cx_index} alarm";
                alarmSequencer.NotifyAlarm(Alarm.Crossroad);
                //return AlarmState.Postponed;
                return false;
            }

            TurnKind? turn_kind = null;
            {
                if (this.crossroadAlarmCount[cx_index] == 1 // we warned the the user, now we are about to give specifics
                    && TurnCalculator.TryComputeTurn(closest_cx, this.trackMap, turn_ahead_distance, out Turn turn))
                {
                    this.lastTurnInfo = (cx_index, turn);
                }
            }

            if (this.crossroadAlarmCount[cx_index] > 0 && this.lastTurnInfo.cxIndex == cx_index)
            {
                Turn turn = this.lastTurnInfo.turn;

                Angle current_bearing = GeoCalculator.GetBearing(somePreviousPoint, currentPoint);
                Angle curr_bearing_to_turn_point = GeoCalculator.GetBearing(currentPoint, closest_cx);
                // completely arbitrary limit of angle, it means 22.5 degrees at each side to qualify as movement towards turning point
                if (GeoCalculator.AbsoluteBearingDifference(current_bearing, curr_bearing_to_turn_point) >= Angle.PI / 4)
                {
                    reason = $"We are leaving turning point";
                    return false;
                }

                if (TryComputeTurnKind(curr_bearing_to_turn_point, turn, out TurnKind tk))
                    turn_kind = tk;
            }


            service.LogDebug(LogLevel.Info, $"Turn at {closest_cx}, dist {min_dist}, repeat {this.crossroadAlarmCount[cx_index]}");

            bool played = false;
            string play_reason;

            if (turn_kind.HasValue)
            {
                played = alarmSequencer.TryAlarm((Alarm)(turn_kind.Value), out play_reason);
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
            return played;// ? AlarmState.Played : AlarmState.Failed;
        }

        internal static bool TryComputeTurnKind(Angle currBearing, in Turn turn, out TurnKind turnKind)
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