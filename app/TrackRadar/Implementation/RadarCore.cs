﻿using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TrackRadar.Implementation
{
    internal sealed class RadarCore : IDisposable
    {
        internal static Length InitialMinAccuracy { get; } = Length.MaxValue;

        internal const string GeoPointFormat = "0.00000000000000";

        private readonly long speedingTicksWindow; // measure speed only when we have time window of X ticks or more

        private bool isRidingWithSpeed;
        // when we walk, we don't alter the speed (sic!), i.e. it is set zero
        internal Speed RidingSpeed { get; private set; }
        private bool engagedState;
        internal bool EngagedState
        {
            get { return engagedState; }
            private set
            {
                this.engagedState = value;
            }
        }
        private bool wasOffTrackPreviously => this.lastOnTrackAlarmAt < this.lastOffTrackAlarmAt;
        private readonly IRadarService service;
        private readonly ISignalCheckerService signalService;
        private readonly IGpsAlarm gpsAlarm;
        private readonly IAlarmSequencer alarmSequencer;
        private readonly ITimeStamper timeStamper;

        private readonly Length startRidingDistance;
        // todo: deal with gps signal loss as well
        private Speed topSpeed;
        private Length totalClimbs;
        private Length overlappingRidingDistance;
        private long ridingPieces;
        private long ridingCount;
        private TimeSpan ridingTime;

        public long StartedAt { get; }

        public TurnLookout Lookout { get; }
        private readonly RoundQueue<GpsReadout> lastPoints;
        private readonly IGeoMap trackMap;
        private long lastOnTrackAlarmAt;
        private long ridingStartedAt;
        private Length? lastAltitude;
        private int offTrackAlarmsCount;
        private long lastOffTrackAlarmAt;
        private Length lastAbsDistance;
        public Length RunningMinAccuracy { get; private set; }
        private int movingOutCount;
        private int movingInCount;
        private OnTrackStatus lastOnTrack;
        private GpsWatchdog gpsWatchdog;

        //private bool postponeSpeedDisengage;

        // todo: this is lame solution, I don't want to recompute the distance each time
        // so I accumulated overlapping segments, so now I have to divide by average number
        // of segments per each GPS update
        // and since starting riding distance is not compatible with overlapping one I need to have two fields :-(
        public Length RidingDistanceReadout
        {
            get
            {
                if (this.ridingPieces == 0)
                    return this.startRidingDistance;
                else
                    return this.startRidingDistance + this.overlappingRidingDistance / (this.ridingPieces * 1.0 / this.ridingCount);
            }
        }
        public TimeSpan RidingTimeReadout
        {
            get
            {
                if (this.ridingStartedAt == timeStamper.GetBeforeTimeTimestamp())
                    return this.ridingTime;
                else
                    return this.ridingTime + TimeSpan.FromSeconds(timeStamper.GetSecondsSpan(ridingStartedAt));

            }
        }
        public Speed TopSpeedReadout => this.topSpeed;
        public Length TotalClimbsReadout => this.totalClimbs;

        public bool HasGpsSignal => this.gpsWatchdog.HasGpsSignal;

        public RadarCore(IRadarService service, ISignalCheckerService signalService, IGpsAlarm gpsAlarm,
            IAlarmSequencer alarmSequencer, ITimeStamper timeStamper,
            IPlanData planData,
            Length totalClimbs, Length startRidingDistance, TimeSpan ridingTime, Speed topSpeed
#if DEBUG
            , Length initMinAccuracy
#endif
           )
        {
            // keeping window of 3 points seems like a good balance for measuring travelled distance (and speed)
            // too wide and we will not get proper speed value when rapidly stopping, 
            // too small and gps accurracy will play major role
            this.lastPoints = new RoundQueue<GpsReadout>(capacity: 3);
            this.lastAbsDistance = Length.PositiveInfinity;
#if DEBUG
            this.RunningMinAccuracy = initMinAccuracy;
#else
            this.RunningMinAccuracy = InitialMinAccuracy;
#endif
            this.service = service;
            this.signalService = signalService;
            this.gpsAlarm = gpsAlarm;
            this.alarmSequencer = alarmSequencer;
            this.timeStamper = timeStamper;
            this.totalClimbs = totalClimbs;
            this.startRidingDistance = startRidingDistance;
            this.overlappingRidingDistance = Length.Zero;
            this.ridingTime = ridingTime;
            this.lastOffTrackAlarmAt = this.lastOnTrackAlarmAt = this.ridingStartedAt = timeStamper.GetBeforeTimeTimestamp();
            this.topSpeed = topSpeed;
            this.speedingTicksWindow = 3 * timeStamper.Frequency; // 3 seconds is good time window
            this.lastOnTrack = OnTrackStatus.OK;
            this.StartedAt = timeStamper.GetTimestamp();

            this.trackMap = CreateTrackMap(planData.Segments);

            this.Lookout = new TurnLookout(service, alarmSequencer, timeStamper, planData, this.trackMap);

            this.alarmSequencer.AlarmPlayed += AlarmSequencer_AlarmPlayed;

            service.LogDebug(LogLevel.Info, $"{trackMap.Segments.Count()} segments in {timeStamper.GetSecondsSpan(StartedAt)}s");
        }

        public void Dispose()
        {
            this.gpsWatchdog?.Dispose();
        }

        public void SetupGpsWatchdog(IPreferences prefs)
        {
            GpsWatchdog watchdog = new GpsWatchdog(signalService, gpsAlarm, this.timeStamper,
                                gpsAcquisitionTimeout: prefs.GpsAcquisitionTimeout,
                                gpsLossTimeout: prefs.GpsLossTimeout,
                                noGpsAgainInterval: prefs.NoGpsAlarmAgainInterval);
            var old_watchdog = Interlocked.Exchange(ref this.gpsWatchdog, watchdog);
            old_watchdog?.Dispose();
            watchdog.Start();
        }

        private void AlarmSequencer_AlarmPlayed(object sender, Alarm alarm)
        {
            if (alarm == Alarm.GoAhead
                || alarm == Alarm.LeftEasy
               || alarm == Alarm.LeftCross
               || alarm == Alarm.LeftSharp
               || alarm == Alarm.RightEasy
               || alarm == Alarm.RightCross
               || alarm == Alarm.RightSharp
               || alarm == Alarm.DoubleTurn
               || alarm == Alarm.Crossroad)
                // if we are within turn reach don't confuse rider with drifting alarms
                this.movingOutCount = 0;
        }

        internal static IGeoMap CreateTrackMap(IEnumerable<ISegment> segments)
        {
            // todo: switch to graph
            // on acquiring gps signal we would need to check entire graph, but after that only last and its
            // adjacent segments
            return GeoMapFactory.CreateGrid(segments);
        }

        public double UpdateLocation(in GeoPoint currentPoint, Length? altitude, Length? accuracy)
        {
            bool gps_acquired = this.gpsWatchdog.UpdateGpsIsOn();
            // if we are in engaged state do not consider gps signal acquiring (i.e. establishing it afresh)
            if (EngagedState)
                gps_acquired = false;
            using (this.alarmSequencer.OpenAlarmContext(gpsAcquired: gps_acquired,
                hasGpsSignal: gpsWatchdog.HasGpsSignal))
            {
                return internalUpdateLocation(currentPoint, altitude, accuracy, gps_acquired);
            }

        }
        /// <returns>negative value means on track</returns>
        private double internalUpdateLocation(in GeoPoint currentPoint, Length? altitude, Length? accuracy, bool gpsAcquired)
        {
            Length fence_dist;
            long now = timeStamper.GetTimestamp();

            OnTrackStatus on_track = isOnTrack(currentPoint, accuracy, out ISegment segment, out fence_dist,
                out ArcSegmentIntersection crosspointInfo);

            this.lastAbsDistance = fence_dist.Abs();

            Speed DEBUG_prevSpeed = this.RidingSpeed;

            {
                Length climb = Length.Zero;
                if (altitude.HasValue && this.lastAltitude.HasValue)
                    climb = altitude.Value - this.lastAltitude.Value;
                if (altitude.HasValue)
                    this.lastAltitude = altitude;

                if (climb > Length.Zero)
                    this.totalClimbs += climb;
            }

            Option<GpsReadout> older_point;

            {
                IEnumerable<GpsReadout> older_points = this.lastPoints.Reverse()
                    .SkipWhile(point_ts => now - point_ts.Timestamp < this.speedingTicksWindow);
                older_point = older_points.FirstOrNone();

                if (!older_point.HasValue)
                {
                    if (this.lastPoints.Count == this.lastPoints.Capacity)
                    {
                        ++this.lastPoints.Capacity;
                        service.LogDebug(LogLevel.Verbose, $"Resizing {nameof(lastPoints)} buffer to {lastPoints.Capacity}");
                    }
                    this.RidingSpeed = Speed.Zero;
                    this.isRidingWithSpeed = false;
                }
                else
                {
                    (GeoPoint last_point, Length? last_alt, Length? last_acc, long last_ts) = older_point.Value;
                    {
                        // treat stored data as a sample of accuracies
                        Option<Length> opt_max_accuracy = older_points
                            .Where(it => it.Accuracy.HasValue)
                            // we call Abs, because we mark with negative sign "borrowed-from" accuracies
                            .Select(it => it.Accuracy.Value.Abs())
                            // yes, take max to elimate min-outliers
                            .MaxOrNone();
                        // at having max from current sample, take the min
                        if (opt_max_accuracy.HasValue)
                        {
                            Length max_accuracy = opt_max_accuracy.Value;
                            // try to add current accuracy
                            if (accuracy.HasValue)
                                max_accuracy = max_accuracy.Max(accuracy.Value);
                            this.RunningMinAccuracy = this.RunningMinAccuracy.Min(max_accuracy);
                        }
                    }

                    double time_s_passed = timeStamper.GetSecondsSpan(now, last_ts);

                    double moved_dist_m = GeoCalculator.GetDistance(last_point, currentPoint).Meters;
                    /*if (last_alt.HasValue && altitude.HasValue)
                    {
                        Length altitude_diff = altitude.Value-last_alt.Value;
#if DEBUG
                        Console.WriteLine($"last acc {last_acc}, curr acc {accuracy}, alt diff {altitude_diff} = {Math.Round(100*altitude_diff.Meters/moved_dist_m)}%");
#endif
                        moved_dist_m = Math.Sqrt(Math.Pow(moved_dist_m, 2) + Math.Pow(altitude_diff.Meters, 2));
                    }*/

                    this.RidingSpeed = Speed.FromMetersPerSecond(moved_dist_m / time_s_passed);
                    bool previously_riding_with_speed = this.isRidingWithSpeed;

                    // we compute engagement speed in order to preserve current state (riding/resting)
                    // this is done to avoid ping-pongs in the forest. Due to bad accuracy GPS
                    // would constantly trigger riding state, and after a while resting, riding again...
                    Speed engagement_speed = this.RidingSpeed;
                    if (this.service.GpsFilter)
                    {
                        if (this.RunningMinAccuracy != Length.MaxValue)
                        {
                            // note Max for last accuracy, if we borrowed from it previously, then now it has negative value
                            Length acc_diff = ((accuracy ?? Length.Zero) + (last_acc ?? Length.Zero).Max(Length.Zero)) / 2 - this.RunningMinAccuracy;

                            if (acc_diff > Length.Zero)
                            {
                                double keep_state_diff = acc_diff.Meters * (previously_riding_with_speed ? 1 : -1);
                                engagement_speed = Speed.FromMetersPerSecond((moved_dist_m + keep_state_diff) / time_s_passed);
                            }
                        }
                    }

                    // if we use only single value (below -- rest, above -- movement) then around this particular value user would have a flood
                    // of notification -- so we use two values, to separate well movement and rest "zones"
                    if (engagement_speed <= service.RestSpeedThreshold)
                    {
                        if (this.ridingStartedAt != timeStamper.GetBeforeTimeTimestamp())
                            this.ridingTime += TimeSpan.FromSeconds(timeStamper.GetSecondsSpan(now, ridingStartedAt));
                        this.ridingStartedAt = timeStamper.GetBeforeTimeTimestamp();
                        this.isRidingWithSpeed = false;
                    }
                    else if (engagement_speed > service.RidingSpeedThreshold)
                    {

                        if (this.ridingStartedAt == timeStamper.GetBeforeTimeTimestamp())
                            this.ridingStartedAt = last_ts;
                        this.isRidingWithSpeed = true;
                    }

                    {
                        // if we didn't change the state at the expense of the accuracy, then alter the accuracy
                        // so we won't continue "borrowing" from it forever
                        bool true_riding = previously_riding_with_speed;
                        if (this.RidingSpeed <= service.RestSpeedThreshold)
                            true_riding = false;
                        else if (this.RidingSpeed > service.RidingSpeedThreshold)
                            true_riding = true;

                        if (this.isRidingWithSpeed != true_riding)
                        {
                            // mark it as borrowed-from one
                            accuracy = -accuracy;
                        }
                    }

                    if (this.isRidingWithSpeed)
                    {
                        this.overlappingRidingDistance += Length.FromMeters(moved_dist_m);
                        this.ridingPieces += older_points.Count() - 1;
                        ++this.ridingCount;
                    }
                    else if (previously_riding_with_speed)
                        this.offTrackAlarmsCount = 0; // treat each stop as fresh start for the off-track counter

                    //   service.LogDebug(LogLevel.Verbose, $"[TEMP] speed: clipped {this.ridingSpeed} computed {curr_speed}, moved {moved_dist} in {time_s_passed}, at {currentPoint.Latitude.Degrees.ToString(GeoPointFormat)} x {currentPoint.Longitude.Degrees.ToString(GeoPointFormat)} from {last_point.Latitude.Degrees.ToString(GeoPointFormat)} x {last_point.Longitude.Degrees.ToString(GeoPointFormat)}");

                    this.topSpeed = this.topSpeed.Max(RidingSpeed);

                    //service.LogDebug(LogLevel.Verbose, $"{(int)service.RestSpeedThreshold.KilometersPerHour}-{(int)service.RidingSpeedThreshold.KilometersPerHour}, fixed time {this.ridingTime.TotalSeconds}, total time {this.RidingTimeReadout.TotalSeconds}");
                }

            }

            this.lastPoints.Enqueue(new GpsReadout(currentPoint, altitude, accuracy: accuracy, timestamp: now));

            this.lastOnTrack = handleAlarm(currentPoint, accuracy, segment, crosspointInfo, on_track, gpsAcquired, DEBUG_prevSpeed, now);

            return fence_dist.Meters;
        }

        private OnTrackStatus handleAlarm(in GeoPoint currentPoint, Length? accuracy,
            ISegment segment, in ArcSegmentIntersection crosspointInfo,
            OnTrackStatus isOnTrack, bool gpsAcquired, Speed DEBUG_prevSpeed, long now)
        {
            if (isOnTrack != OnTrackStatus.OffTrack)
            {
                if (Lookout.AlarmTurnAhead(currentPoint, segment, crosspointInfo, this.isRidingWithSpeed ? this.RidingSpeed : Speed.Zero, wasOffTrackPreviously, now, out string _))
                {
                    this.offTrackAlarmsCount = 0;
                    EngagedState = true;
                    this.lastOnTrackAlarmAt = now;
                    isOnTrack = OnTrackStatus.OK;
                }

            }

            if (gpsAcquired && isOnTrack != OnTrackStatus.OK)
            {
                // if we just got GPS signal but we are off-track we need to play off-track signal, not gps-acquired
                alarmOffTrack(currentPoint, isOnTrack, now);
            }
            // do not trigger alarm if we stopped moving
            else if (!this.isRidingWithSpeed)
            {
                // here we check the interval to prevent too often playing ACK
                // possible case: user got back on track and then stopped, without checking interval she/he would got two ACKs
                if (EngagedState
                   //prevRiding != Speed.Zero
                   // reusing off-track interval, no point making separate settings
                   //               && timeStamper.GetSecondsSpan(now, this.lastOnTrackAlarmAt) >= service.OffTrackAlarmInterval.TotalSeconds
                   )
                {
                    {
                        const Alarm alarm = Alarm.Disengage;
                        bool played = alarmSequencer.TryAlarm(alarm, false, out string reason);
                        service.LogDebug(LogLevel.Verbose, $"Disengage played {played}, reason {reason}, stopped, previous speed {DEBUG_prevSpeed.KilometersPerHour} km/h");

                        if (played)
                        {
                            service.WriteDebug(currentPoint.Latitude.Degrees, currentPoint.Longitude.Degrees, alarm.ToString(), $"acc: {accuracy}");
                            EngagedState = false;
                        }
                    }
                }
            }
            else if (isOnTrack == OnTrackStatus.OK) // we are riding "full" speed
            {
                this.offTrackAlarmsCount = 0;

                if (!EngagedState// we started riding, engagement
                    || wasOffTrackPreviously) // we were previously off-track
                {
                    //var alarm = prevRiding == Speed.Zero ? Alarm.Engaged : Alarm.BackOnTrack;
                    var alarm = wasOffTrackPreviously ? Alarm.BackOnTrack : Alarm.Engaged;
                    var played = alarmSequencer.TryAlarm(alarm, false, out string reason);
                    service.LogDebug(LogLevel.Verbose, $"{alarm} played {played}, reason: {reason}, speed {this.RidingSpeed.KilometersPerHour} km/h");

                    if (played)
                    {
                        service.WriteDebug(currentPoint.Latitude.Degrees, currentPoint.Longitude.Degrees, alarm.ToString(), $"acc: {accuracy}");
                        this.lastOnTrackAlarmAt = now;
                        EngagedState = true;
                        // this.postponeSpeedDisengage = false;
                    }
                }

            }
            else
            {
                alarmOffTrack(currentPoint, isOnTrack, now);
            }

            return isOnTrack;
        }

        private void alarmOffTrack(GeoPoint currentPoint, OnTrackStatus isOnTrack, long now)
        {
            // alarm = Alarm.OffTrack;

            // we are off-track but we cannot alarm about it (because of the count limit, or interval)
            if (offTrackAlarmsCount > this.service.OffTrackAlarmCountLimit ||
                timeStamper.GetSecondsSpan(now, this.lastOffTrackAlarmAt) < service.OffTrackAlarmInterval.TotalSeconds)
            {
                this.alarmSequencer.NotifyAlarm(Alarm.OffTrack);
            }
            else
            {
                // technically we are off-track, but if didn't alarm user we disengaged because of the speed
                // (user is walking, usually sightseeing)
                /*if (false && this.postponeSpeedDisengage)
                {
                    bool played = alarmSequencer.TryAlarm(Alarm.Disengage, out string reason);
                    service.LogDebug(LogLevel.Verbose, $"Disengage played {played}, reason {reason}, stopped, previously riding {prevRiding}m/s");

                    if (played)
                    {
                        engagedState = false;
                        this.postponeSpeedDisengage = false;
                    }
                }
                else*/
                {
                    // do NOT try to be smart, and check if we are closing to the any of the tracks, this is because in real life
                    // we can be closing to parallel track however with some fence between us, so when we are off the track
                    // we are OFF THE TRACK and alarm the user about it -- user has info about environment, she/he sees if it possible
                    // to take a shortcut, we don't see a thing

                    Alarm alarm = offTrackAlarmsCount == this.service.OffTrackAlarmCountLimit ? Alarm.Disengage : Alarm.OffTrack;
                    var played = alarmSequencer.TryAlarm(alarm, false, out string reason);
                    if (played)
                    {
                        this.lastOffTrackAlarmAt = now;
                        ++this.offTrackAlarmsCount;
                        // it might happen, that off-track is the very first alarm, thus engaging 
                        EngagedState = alarm == Alarm.OffTrack;
                    }
                    else
                        service.LogDebug(LogLevel.Warning, $"Off-track alarm, couldn't play, reason {reason}");

                    // it should be easier to make a GPX file out of it (we don't create it here because service crashes too often)
                    service.WriteOffTrack(latitudeDegrees: currentPoint.Latitude.Degrees,
                        longitudeDegrees: currentPoint.Longitude.Degrees,
                        name: $"{isOnTrack} {offTrackAlarmsCount} at speed {(this.RidingSpeed.KilometersPerHour.ToString("0.##"))}");
                }
            }
        }


        /// <param name="fence_dist">negative value means on track</param>
        private OnTrackStatus isOnTrack(in GeoPoint point, Length? accuracy, out ISegment segment, out Length fence_dist,
            out ArcSegmentIntersection crosspointInfo)
        {
            Length limit = service.OffTrackAlarmDistance;
            if (accuracy.HasValue)
                limit += accuracy.Value;
            bool on_track = PositionCalculator.IsOnTrack(point, trackMap, limit, out segment, out fence_dist, out crosspointInfo);
            //service.Verbose($"Distance {fence_dist.Abs()}");
            if (!on_track)
                return OnTrackStatus.OffTrack;

            if (on_track) // DRIFT LOGIC
            {
                if (lastAbsDistance.IsPositiveInfinity || fence_dist.IsPositiveInfinity)
                {
                    this.movingInCount = 0;
                    this.movingOutCount = 0;
                }
                else
                {
                    if (fence_dist.Abs() > lastAbsDistance)
                        this.movingInCount = 0;
                    if (fence_dist.Abs() <= service.DriftWarningDistance)
                        this.movingOutCount = 0;

                    if (fence_dist.Abs() < lastAbsDistance) // moving in
                    {
                        this.movingOutCount = Math.Max(0, this.movingOutCount - 1);
                        ++this.movingInCount;

                        if (this.movingInCount > service.DriftComingCloserCountLimit)
                        {
                            if (fence_dist.Abs() <= service.DriftWarningDistance)
                                return OnTrackStatus.OK;
                        }
                    }

                    // moving away
                    if (fence_dist.Abs() > lastAbsDistance && fence_dist.Abs() > service.DriftWarningDistance)
                    {
                        ++this.movingOutCount;

                        if (this.movingOutCount > service.DriftMovingAwayCountLimit)
                        {
                            this.movingOutCount = 0;
                            return OnTrackStatus.Drifting;
                        }
                    }
                }

                // if we couldn't prove for sure we are on track, keep previous state
                if (this.lastOnTrack == OnTrackStatus.Drifting)
                    return this.lastOnTrack;

            }

            return OnTrackStatus.OK;
        }

        /*       internal void UpdateGpsPendingAlarm(bool gpsAcquired, bool hasGpsSignal)
               {
                   if (gpsAcquired)
                   {
                       if (!this.pendingAlarm.HasValue)
                           this.pendingAlarm = Alarm.GpsAcquired;
                   }
                   else if (!hasGpsSignal)
                   {
                       if (this.pendingAlarm == Alarm.GpsAcquired)
                           this.pendingAlarm = null;
                   }

               }*/
    }
}