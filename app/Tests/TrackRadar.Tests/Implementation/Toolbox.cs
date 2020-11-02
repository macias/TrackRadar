using Geo;
using Gpx;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TrackRadar.Implementation;
using static TrackRadar.GpxLoader;

namespace TrackRadar.Tests.Implementation
{
    public static class Toolbox
    {
        /* public static (double maxUpdate, double avgUpdate) Ride(Preferences prefs, string planFilename, string trackedFilename,
             out IReadOnlyDictionary<Alarm, int> alarmCounters,
             out IReadOnlyList<(Alarm alarm, int index)> alarms,
             out IReadOnlyList<(string message, int index)> messages)
         {
             return Ride(prefs, planFilename, trackedFilename, null, out alarmCounters, out alarms, out messages);
         }
         */
        public static (double maxUpdate, double avgUpdate) Ride(Preferences prefs, string planFilename, string trackedFilename,
            Speed? speed,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages,
            bool reverse = false)
        {
            LoadData(prefs, planFilename, trackedFilename,
                out IPlanData plan_data, out List<GeoPoint> track_points);

            if (speed != null)
                PopulateTrackDensely(track_points, speed.Value);

            if (reverse)
                track_points.Reverse();

            return Toolbox.Ride(prefs, plan_data, track_points, out alarmCounters, out alarms, out messages);

        }

        public static void LoadData(Preferences prefs, string planFilename, string trackedFilename,
            out IPlanData planData, out List<GeoPoint> trackPoints)
        {
            planData = LoadPlan(prefs, planFilename);
            // we assume for reals rides GPS acquire interval was one second, thus we don't have to process timestamps
            // because 1 second is our test interval
            trackPoints = ReadTrackPoints(trackedFilename).ToList();
        }

        public static IEnumerable<GeoPoint> ReadTrackPoints(string trackedFilename)
        {
            return Toolbox.ReadTrackGpxPoints(trackedFilename).Select(it => GpxHelper.FromGpx(it));
        }

        public static Action<double> OnProgressValidator()
        {
            double last_seen = 0;
            return new Action<double>(x =>
            {
                if (x < last_seen)
                    throw new ArgumentException($"Progress cannot go down, last {last_seen}, current {x}");
                last_seen = x;
            });
        }

        internal static IPlanData LoadPlan(Preferences prefs, string planFilename)
        {
            return GpxLoader.ReadGpx(planFilename, prefs.OffTrackAlarmDistance, onProgress: OnProgressValidator(), CancellationToken.None);
        }
        public static (double maxUpdate, double avgUpdate) Ride(Preferences prefs, IPlanData planData,
            List<GeoPoint> trackPoints,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages)
        {
            return Ride(prefs, planData, trackPoints, out alarmCounters, out alarms, out messages, out _);
        }

        internal static (double maxUpdate, double avgUpdate) Ride(Preferences prefs, IPlanData planData,
            List<GeoPoint> trackPoints,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages,
            out TurnLookout lookout)
        {

            var clock = new SecondStamper();
            using (var raw_alarm_master = new AlarmMaster(clock))
            {
                raw_alarm_master.PopulateAlarms();

                var counting_alarm_master = new CountingAlarmMaster(raw_alarm_master);

                var service = new TrackRadar.Tests.Implementation.RadarService(prefs, clock);
                AlarmSequencer sequencer = new AlarmSequencer(service, counting_alarm_master);
                var core = new RadarCore(service, sequencer, clock, planData, Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero);
                lookout = core.Lookout;

                int point_index = 0;
                long longest_update = 0;
                long start_all = Stopwatch.GetTimestamp();
                foreach (GeoPoint pt in trackPoints)
                {
                    using (sequencer.OpenAlarmContext(gpsAcquired: false, hasGpsSignal: true))
                    {
                        if (point_index == 45)
                        {
                            ;
                        }
                        counting_alarm_master.SetPointIndex(point_index);
                        long start = Stopwatch.GetTimestamp();
                        core.UpdateLocation(pt, null, accuracy: null);
                        long passed = Stopwatch.GetTimestamp() - start;
                        if (longest_update < passed)
                            longest_update = passed;
                        clock.Advance();
                        ++point_index;
                    }
                }

                alarmCounters = counting_alarm_master.AlarmCounters;
                alarms = counting_alarm_master.Alarms;
                messages = counting_alarm_master.Messages;

                return (longest_update * 1.0 / Stopwatch.Frequency,
                    (Stopwatch.GetTimestamp() - start_all - 0.0) / (Stopwatch.Frequency * trackPoints.Count));
            }
        }

        public static Preferences CreatePreferences()
        {
            return new Preferences() { TurnAheadAlarmDistance = TimeSpan.FromSeconds(17) };
        }

        internal static Preferences LowThresholdSpeedPreferences()
        {
            var prefs = CreatePreferences();
            prefs.RestSpeedThreshold = Speed.Zero;
            prefs.RidingSpeedThreshold = Speed.FromMetersPerSecond(1);
            return prefs;
        }


        public static void SaveGpxSegments(string filename, IEnumerable<ISegment> segments)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (ISegment seg in segments)
                {
                    writer.WriteTrack($"{idx}:{seg.SectionId}", seg.Points().ToArray());
                    ++idx;
                }
            }
        }
        public static void SaveGpx(string filename, IPlanData plan)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                {
                    int idx = 0;
                    foreach (ISegment seg in plan.Segments)
                    {
#if DEBUG
                        writer.WriteTrack($"Line {idx}:{seg.SectionId} #{seg.__DEBUG_id}", seg.Points().ToArray());
#else
                        writer.WriteTrack($"Line {idx}:{seg.SectionId}", seg.Points().ToArray());
#endif
                        ++idx;
                    }
                }
                {
                    foreach (var cx_entry in plan.Crossroads)
                    {
                        writer.WritePoint($"Point {cx_entry.Value}", cx_entry.Key);
                    }
                }
            }
        }

        public static IReadOnlyList<GeoPoint> GetCrossroadsList(this IPlanData plan)
        {
            return plan.Crossroads.OrderBy(it => it.Value).Select(it => it.Key).ToList();
        }
        public static void SaveGpx(string filename, IEnumerable<IEnumerable<GeoPoint>> segments,
            IEnumerable<GeoPoint> waypoints)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                {
                    int idx = 0;
                    foreach (IEnumerable<GeoPoint> seg in segments)
                    {
                        writer.WriteTrack($"Line {idx}", seg.ToArray());
                        ++idx;
                    }
                }
                {
                    int idx = 0;
                    foreach (GeoPoint pt in waypoints)
                    {
                        writer.WritePoint($"Point {idx}", pt);
                        ++idx;
                    }
                }
            }
        }
        public static void SaveGpxSegments(string filename, params IEnumerable<GeoPoint>[] segments)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (IEnumerable<GeoPoint> seg in segments)
                {
                    writer.WriteTrack($"Line {idx}", seg.ToArray());
                    ++idx;
                }
            }
        }
        public static void SaveGpxWaypoints(string filename, IEnumerable<GeoPoint> points)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (GeoPoint pt in points)
                {
                    writer.WritePoint($"{idx}", pt);
                    ++idx;
                }
            }
        }

        internal static void PopulateAlarms(this AlarmMaster alarmMaster)
        {
            alarmMaster.Reset(new TestAlarmVibrator(),
                offTrackPlayer: new TestAlarmPlayer(AlarmSound.OffTrack),
                gpsLostPlayer: new TestAlarmPlayer(AlarmSound.GpsLost),
                gpsOnPlayer: new TestAlarmPlayer(AlarmSound.BackOnTrack),
                disengage: new TestAlarmPlayer(AlarmSound.Disengage),
                crossroadsPlayer: new TestAlarmPlayer(AlarmSound.Crossroad),
                goAhead: new TestAlarmPlayer(AlarmSound.GoAhead),
                leftEasy: new TestAlarmPlayer(AlarmSound.LeftEasy),
                leftCross: new TestAlarmPlayer(AlarmSound.LeftCross),
                leftSharp: new TestAlarmPlayer(AlarmSound.LeftSharp),
                rightEasy: new TestAlarmPlayer(AlarmSound.RightEasy),
                rightCross: new TestAlarmPlayer(AlarmSound.RightCross),
                rightSharp: new TestAlarmPlayer(AlarmSound.RightSharp),
                doubleTurn: new TestAlarmPlayer(AlarmSound.DoubleTurn));
        }

        public static void PopulateTrackDensely(List<GeoPoint> trackPoints)
        {
            PopulateTrackDensely(trackPoints, Speed.FromMetersPerSecond(3));
        }

        public static void PopulateTrackDensely(List<GeoPoint> trackPoints, Speed speed)
        {
            for (int i = 0; i < trackPoints.Count - 1; ++i)
            {
                while (GeoCalculator.GetDistance(trackPoints[i], trackPoints[i + 1]).Meters > speed.MetersPerSecond)
                    trackPoints.Insert(i + 1, GeoCalculator.GetMidPoint(trackPoints[i], trackPoints[i + 1]));
            }
        }

        public static IEnumerable<GpxTrackPoint> ReadTrackGpxPoints(string rideFilename)
        {
            var track_points = new List<GpxTrackPoint>();
            using (Gpx.GpxIOFactory.CreateReader(rideFilename, out IGpxReader reader, out _))
            {
                while (reader.Read(out GpxObjectType type))
                {
                    if (type == GpxObjectType.Track)
                    {
                        track_points.AddRange(reader.Track.Segments.SelectMany(it => it.TrackPoints));
                    }
                }

            }

            return track_points;
        }

        public static IPlanData CreateTrackData(IEnumerable<GeoPoint> track, IEnumerable<GeoPoint> waypoints, Length offTrackDistance)
        {
            return CreateTrackData(waypoints, offTrackDistance, track);
        }
        public static IPlanData CreateTrackData(IEnumerable<GeoPoint> waypoints, Length offTrackDistance,
            params IEnumerable<GeoPoint>[] tracks)
        {
            return GpxLoader.ProcessTrackData(tracks, waypoints ?? Enumerable.Empty<GeoPoint>(), offTrackDistance: offTrackDistance,
                segmentLengthLimit: GeoMapFactory.SegmentLengthLimit, null, CancellationToken.None);
        }

        public static IGeoMap CreateTrackMap(IEnumerable<GeoPoint> track)
        {
            var prefs = Toolbox.CreatePreferences();
            IPlanData track_data = CreateTrackData(track, new GeoPoint[] { }, prefs.OffTrackAlarmDistance);
            return RadarCore.CreateTrackMap(track_data.Segments);
        }

        internal static IEnumerable<GpxWayPoint> ReadWaypoints(string ride_filename)
        {
            var way_points = new List<GpxWayPoint>();
            using (Gpx.GpxIOFactory.CreateReader(ride_filename, out IGpxReader reader, out _))
            {
                while (reader.Read(out GpxObjectType type))
                {
                    if (type == GpxObjectType.WayPoint)
                    {
                        way_points.Add(reader.WayPoint);
                    }
                }

            }

            return way_points;
        }

    }
}