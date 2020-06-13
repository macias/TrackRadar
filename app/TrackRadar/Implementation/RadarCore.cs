using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    internal sealed class RadarCore
    {
        internal const string GeoPointFormat = "0.00000000000000";

        private readonly long speedTicksWindow; // measure speed only when we have time window of X ticks or more

        // when we walk, we don't alter the speed (sic!)
        private Speed ridingSpeed;
        private const int crossroadsWarningLimit = 3;
        private readonly IRadarService service;
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


        private readonly RoundQueue<(GeoPoint point, Length? altitude, long timestamp)> lastPoints;
        private readonly IGeoMap trackMap;
        private readonly IReadOnlyList<GeoPoint> trackCrossroads;
        private long lastTurnAheadAlarmAt;
        private readonly List<int> alarmedCrossroads;

        // asymmetric behavior for this flag -- when on track, set it right away, 
        // but if off track, set in only when we trigger alarm
        // rationale: when we are back on track playing ACK has only sense if we played OFF before
        // otherwise we would surprise user with ACK against no  previously existing alarm
        private bool lastReportedOnTrack;
        private long lastOffTrackAlarmAt;
        private long ridingStarterAt;
        private Length? lastAltitude;

        // todo: this is lame solution, I don't want to recompute the distance each time
        // so I accumulated overlapping segments, so now I have to divide by average number
        // of segments per each GPS update
        // and since starting riding distance is not compatible with overlapping one I need to have two fields :-(
        public Length RidingDistanceReadout
        {
            get
            {
                if (this.ridingCount == 0)
                    return this.startRidingDistance;
                else
                    return this.startRidingDistance + this.overlappingRidingDistance / (this.ridingPieces * 1.0 / this.ridingCount);
            }
        }
        public TimeSpan RidingTimeReadout
        {
            get
            {
                if (this.ridingStarterAt == 0)
                    return this.ridingTime;
                else
                    return this.ridingTime + TimeSpan.FromSeconds(timeStamper.GetSecondsSpan(ridingStarterAt));

            }
        }
        public Speed TopSpeedReadout => this.topSpeed;
        public Length TotalClimbsReadout => this.totalClimbs;

        public RadarCore(IRadarService service, ITimeStamper timeStamper, GpxData gpxData,
            Length totalClimbs, Length startRidingDistance, TimeSpan ridingTime, Speed topSpeed)
        {
            // keeping window of 3 points seems like a good balance for measuring travelled distance (and speed)
            // too wide and we will not get proper speed value when rapidly stopping, 
            // too small and gps accurracy will play major role
            this.lastPoints = new RoundQueue<(GeoPoint point, Length? altitude, long timestamp)>(capacity: 3);
            this.service = service;
            this.timeStamper = timeStamper;
            this.totalClimbs = totalClimbs;
            this.startRidingDistance = startRidingDistance;
            this.overlappingRidingDistance = Length.Zero;
            this.ridingTime = ridingTime;
            this.topSpeed = topSpeed;
            this.speedTicksWindow = 3 * timeStamper.Frequency; // 3 seconds is good time window

            this.StartedAt = timeStamper.GetTimestamp();

            this.trackMap = GeoMapFactory.CreateGrid(gpxData.Segments,
                (_, p) => p,
                (_, a, b) => new Segment(a, b),
                GeoMapFactory.GridLimit);
            this.trackCrossroads = gpxData.Crossroads.ToList();
            this.alarmedCrossroads = gpxData.Crossroads.Select(_ => 0).ToList();

            service.LogDebug(LogLevel.Info, $"{trackMap.Segments.Count()} segments in {timeStamper.GetSecondsSpan(StartedAt)}s");
        }



        /// <returns>negative value means on track</returns>
        public double UpdateLocation(in GeoPoint currentPoint, Length? altitude, float accuracy)
        {
            double dist;
            long now = timeStamper.GetTimestamp();

            Length accuracy_len = Length.FromMeters(accuracy);


            bool on_track = isOnTrack(currentPoint, accuracy_len, out dist);

            Speed prev_riding = this.ridingSpeed;

            {
                Length climb = Length.Zero;
                if (altitude.HasValue && this.lastAltitude.HasValue)
                    climb = altitude.Value - this.lastAltitude.Value;
                if (altitude.HasValue)
                    this.lastAltitude = altitude;

                if (climb > Length.Zero)
                    this.totalClimbs += climb;
            }

            {
                IEnumerable<(GeoPoint point, Length? altitude, long timestamp)> loc_pieces = this.lastPoints.Reverse()
                    .SkipWhile(point_ts => now - point_ts.timestamp < this.speedTicksWindow);
                Option<(GeoPoint, Length?, long)> opt = loc_pieces.FirstOrNone();

                if (!opt.HasValue)
                {
                    if (this.lastPoints.Count == this.lastPoints.Capacity)
                    {
                        ++this.lastPoints.Capacity;
                        service.LogDebug(LogLevel.Verbose, $"Resizing {nameof(lastPoints)} buffer to {lastPoints.Capacity}");
                    }
                    this.ridingSpeed = Speed.Zero;
                }
                else
                {
                    (GeoPoint last_point, Length? last_alt, long last_ts) = opt.Value;

                    double time_s_passed = timeStamper.GetSecondsSpan(now, last_ts);

                    double moved_dist_m = GeoCalculator.GetDistance(currentPoint, last_point).Meters;
                    if (last_alt.HasValue && altitude.HasValue)
                        moved_dist_m = Math.Sqrt(Math.Pow(moved_dist_m, 2) + Math.Pow(last_alt.Value.Meters - altitude.Value.Meters, 2));

                    Speed curr_speed = Speed.FromMetersPerSecond(moved_dist_m / time_s_passed);
                    // if we use only single value (below -- rest, above -- movement) then around this particular value user would have a flood
                    // of notification -- so we use two values, to separate well movement and rest "zones"
                    if (curr_speed <= service.RestSpeedThreshold)
                    {
                        if (this.ridingStarterAt != 0)
                            this.ridingTime += TimeSpan.FromSeconds(timeStamper.GetSecondsSpan(now, ridingStarterAt));
                        this.ridingStarterAt = 0;
                        this.ridingSpeed = Speed.Zero;
                    }
                    else if (curr_speed > service.RidingSpeedThreshold)
                    {
                        if (this.ridingStarterAt == 0)
                            this.ridingStarterAt = last_ts;
                        this.ridingSpeed = curr_speed;
                    }

                    if (this.ridingSpeed != Speed.Zero)
                    {
                        this.overlappingRidingDistance += Length.FromMeters(moved_dist_m);
                        this.ridingPieces += loc_pieces.Count() - 1;
                        ++this.ridingCount;
                    }
                    //   service.LogDebug(LogLevel.Verbose, $"[TEMP] speed: clipped {this.ridingSpeed} computed {curr_speed}, moved {moved_dist} in {time_s_passed}, at {currentPoint.Latitude.Degrees.ToString(GeoPointFormat)} x {currentPoint.Longitude.Degrees.ToString(GeoPointFormat)} from {last_point.Latitude.Degrees.ToString(GeoPointFormat)} x {last_point.Longitude.Degrees.ToString(GeoPointFormat)}");

                    this.topSpeed = this.topSpeed.Max(curr_speed);

                    service.LogDebug(LogLevel.Verbose, $"{(int)service.RestSpeedThreshold.KilometersPerHour}-{(int)service.RidingSpeedThreshold.KilometersPerHour}, fixed time {this.ridingTime.TotalSeconds}, total time {this.RidingTimeReadout.TotalSeconds}");
                }
            }

            this.lastPoints.Enqueue((currentPoint, altitude, now));

            if (on_track)
            {
                if (!this.lastReportedOnTrack)
                {
                    bool played = service.TryAlarm(Alarm.PositiveAcknowledgement, out string reason);
                    service.LogDebug(LogLevel.Verbose, $"ACK played {played}, reason: {reason}, back on track");

                    this.lastReportedOnTrack = played;
                }
                else if (prev_riding > Speed.Zero && this.ridingSpeed == Speed.Zero)
                {
                    bool played = service.TryAlarm(Alarm.PositiveAcknowledgement, out string reason);
                    service.LogDebug(LogLevel.Verbose, $"ACK played {played}, reason {reason}, stopped, previously riding {prev_riding}m/s");
                }

                if (this.ridingSpeed != Speed.Zero && alarmTurnAhead(currentPoint, accuracy_len, now))
                    service.WriteCrossroad(latitudeDegrees: currentPoint.Latitude.Degrees, longitudeDegrees: currentPoint.Longitude.Degrees);

                return dist;
            }
            else
            {

                // do not trigger alarm if we stopped moving
                if (this.ridingSpeed == Speed.Zero)
                    return dist;

                var passed = timeStamper.GetSecondsSpan(now, this.lastOffTrackAlarmAt);
                if (passed < service.OffTrackAlarmInterval.TotalSeconds)
                    return dist;

                // do NOT try to be smart, and check if we are closing to the any of the tracks, this is because in real life
                // we can be closing to parallel track however with some fence between us, so when we are off the track
                // we are OFF THE TRACK and alarm the user about it -- user has info about environment, she/he sees if it possible
                // to take a shortcut, we don't see a thing

                this.lastReportedOnTrack = false;

                if (service.TryAlarm(Alarm.OffTrack, out string reason))
                    this.lastOffTrackAlarmAt = now;
                else
                    service.LogDebug(LogLevel.Warning, $"Off-track alarm, couldn't play, reason {reason}");

                // it should be easier to make a GPX file out of it (we don't create it here because service crashes too often)
                service.WriteOffTrack(latitudeDegrees: currentPoint.Latitude.Degrees, longitudeDegrees: currentPoint.Longitude.Degrees,
                    $"speed {this.ridingSpeed}, rest {service.RestSpeedThreshold}, ride {service.RidingSpeedThreshold}");

                return dist;
            }
        }

        private bool alarmTurnAhead(GeoPoint point, Length accuracy, long now)
        {
            if (service.TurnAheadAlarmDistance == Length.Zero)
                return false;

            var passed = timeStamper.GetSecondsSpan(now, this.lastTurnAheadAlarmAt);

            if (passed < service.TurnAheadAlarmInterval.TotalSeconds)
                return false;

            Length turn_ahead_distance = service.TurnAheadAlarmDistance;

            Length min_dist = Length.MaxValue;
            int closest_idx = -1;

            for (int i = 0; i < this.trackCrossroads.Count; ++i)
            {
                Length dist = GeoCalculator.GetDistance(this.trackCrossroads[i], point);
                if (dist - accuracy > turn_ahead_distance * 2)
                {
                    this.alarmedCrossroads[i] = 0;
                }
                else if (dist < min_dist)
                {
                    min_dist = dist;
                    closest_idx = i;
                }
            }

            bool played = false;

            if (closest_idx != -1 && min_dist <= turn_ahead_distance && this.alarmedCrossroads[closest_idx] < crossroadsWarningLimit)
            {
                service.LogDebug(LogLevel.Info, $"Turn at {trackCrossroads[closest_idx]}, dist {min_dist}, repeat {this.alarmedCrossroads[closest_idx]}");
                played = service.TryAlarm(Alarm.Crossroad, out string reason);

                if (played)
                {
                    this.lastTurnAheadAlarmAt = now;
                    ++this.alarmedCrossroads[closest_idx];
                }
                else
                    service.LogDebug(LogLevel.Warning, $"Turn ahead alarm, couldn't play, reason {reason}");
            }

            return played;
        }



        /// <param name="dist">negative value means on track</param>
        private bool isOnTrack(in GeoPoint point, Length accuracy, out double dist)
        {
            return PositionCalculator.IsOnTrack(point, trackMap,
                service.OffTrackAlarmDistance + accuracy, out dist);
        }

    }
}