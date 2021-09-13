using Geo;
using Gpx;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TrackRadar.Implementation;
using static TrackRadar.Implementation.GpxLoader;

namespace TrackRadar.Tests.Implementation
{
    public static class Toolbox
    {
#if DEBUG
        public static void SaveGraph(string filename, ITurnGraph graph)
        {
            if (System.IO.File.Exists(filename))
                throw new ArgumentException($"File {filename} already exists");

            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                // point -> index
                var turn_points = new Dictionary<GeoPoint, int>();
                foreach (GeoPoint pt in graph.DEBUG_TurnPoints)
                {
                    writer.WriteWaypoint(pt, turn_points.Count.ToString());
                    turn_points.Add(pt, turn_points.Count);
                }

                foreach (DEBUG_TrackToTurnHack assignment in graph.DEBUG_TrackToTurnPoints)
                {
                    string info = $"{turn_points[assignment.Primary.TurnPoint]} {(assignment.Primary.Distance.Meters.ToString("0"))}";
                    if (assignment.Alternate.HasValue)
                        info += $", {turn_points[assignment.Alternate.Value.TurnPoint]} {(assignment.Alternate.Value.Distance.Meters.ToString("0"))}";
                    writer.WriteWaypoint(assignment.TrackPoint, info);
                }
            }
        }
#endif

        public static string TestData(string filename)
        {
            return System.IO.Path.Combine("Data/", filename);
        }
        public static RideStats Ride(Preferences prefs, string planFilename, string trackedFilename,
            Speed? speed,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages,
            bool reverse = false)
        {
            LoadData(prefs, planFilename, trackedFilename,
                out IPlanData plan_data, out List<GpsPoint> track_points);

            if (speed != null)
                track_points = PopulateTrackDensely(track_points, speed.Value);

            if (reverse)
                track_points.Reverse();

            return Toolbox.Ride(prefs, plan_data, track_points, out alarmCounters, out alarms, out messages);

        }

        public static RideStats Ride(Preferences prefs, TimeSpan playDuration, string planFilename, string trackedFilename,
            Speed? speed,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages,
            bool reverse = false)
        {
            var result = Ride(prefs, playDuration, planFilename, trackedFilename, speed, reverse);
            alarmCounters = result.AlarmCounters;
            alarms = result.Alarms;
            messages = result.Messages;
            return result;
        }

        public static RideStats Ride(Preferences prefs, TimeSpan playDuration, string planFilename, string trackedFilename,
            Speed? speed,
            bool reverse = false)
        {
            return RideLogged(
#if DEBUG
                MetaLogger.None,
#endif
                prefs, playDuration, planFilename, trackedFilename, speed, reverse);
        }

        internal static RideStats RideLogged(
#if DEBUG
            MetaLogger DEBUG_logger,
#endif
            Preferences prefs, TimeSpan playDuration, string planFilename, string trackedFilename,
            Speed? speed, bool reverse = false)
        {
            LoadDataLogged(
#if DEBUG
                DEBUG_logger,
#endif
                prefs, planFilename, trackedFilename,
                out IPlanData plan_data, out List<GpsPoint> track_points);

            if (speed != null)
                track_points = PopulateTrackDensely(track_points, speed.Value);

            if (reverse)
                track_points.Reverse();

            return Ride(
                prefs, playDuration, plan_data, track_points, out _, out _, out _, out _);

        }

#if DEBUG
        internal static void PrintAlarms(in RideStats stats)
        {
            PrintAlarms(stats.Alarms, statsPrefix: true);
        }

        internal static void PrintAlarms(IReadOnlyList<(Alarm alarm, int index)> alarms, bool statsPrefix = false)
        {
            Console.WriteLine($"Assert.AreEqual({alarms.Count}, {(statsPrefix ? "stats.A" : "a")}larms.Count);");
            Console.WriteLine("int a = 0;");
            Console.WriteLine();

            foreach (var alarm in alarms)
            {
                string line = $"Assert.AreEqual(({alarm.alarm.GetType().Name}.{alarm.alarm}, {alarm.index}), {(statsPrefix ? "stats.A" : "a")}larms[a++]);";
                Console.WriteLine(line);
                Debug.WriteLine(line);
            }
        }
#endif

        internal static void LoadData(Preferences prefs, string planFilename, string trackedFilename,
            out IPlanData planData, out List<GpsPoint> trackPoints)
        {
            LoadDataLogged(
#if DEBUG
                MetaLogger.None,
#endif
                prefs, planFilename, trackedFilename, out planData, out trackPoints);
        }

        internal static void LoadDataLogged(
#if DEBUG
            MetaLogger DEBUG_logger,
#endif
            Preferences prefs, string planFilename, string trackedFilename,
    out IPlanData planData, out List<GpsPoint> trackPoints)
        {
            planData = LoadPlanLogged(
#if DEBUG
                DEBUG_logger,
#endif
                prefs, planFilename);
            // we assume for reals rides GPS acquire interval was one second, thus we don't have to process timestamps
            // because 1 second is our test interval
            trackPoints = ReadTrackPoints(trackedFilename).ToList();
        }

        public static IEnumerable<GpsPoint> ReadTrackPoints(string trackedFilename)
        {
            return Toolbox.ReadTrackGpxPoints(trackedFilename).Select(it => new GpsPoint(it));
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

        internal static IPlanData LoadPlanLogged(
#if DEBUG
            MetaLogger DEBUG_logger,
#endif
            Preferences prefs, string planFilename)
        {
            return GpxLoader.ReadGpx(
#if DEBUG
                DEBUG_logger,
#endif
                planFilename, prefs.OffTrackAlarmDistance, onProgress: OnProgressValidator(), CancellationToken.None);
        }

        internal static IPlanData LoadPlan(Preferences prefs, string planFilename)
        {
            return LoadPlanLogged(
#if DEBUG
                MetaLogger.None,
#endif
                prefs, planFilename);
        }

        public static RideStats Ride(Preferences prefs, IPlanData planData,
            IReadOnlyList<GpsPoint> trackPoints,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages)
        {
            return Ride(prefs, playDuration: null, planData, trackPoints, out alarmCounters, out alarms, out messages, out _);
        }

        public static RideStats Ride(Preferences prefs, IPlanData planData,
    IReadOnlyList<GpsPoint?> trackPoints,
    out IReadOnlyDictionary<Alarm, int> alarmCounters,
    out IReadOnlyList<(Alarm alarm, int index)> alarms,
    out IReadOnlyList<(string message, int index)> messages)
        {
            return RideWithGps(prefs, playDuration: null, planData, trackPoints, out alarmCounters, out alarms, out messages, out _);
        }

        public static RideStats Ride(Preferences prefs, TimeSpan? playDuration, IPlanData planData,
            IReadOnlyList<GpsPoint> trackPoints,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages)
        {
            return Ride(prefs, playDuration, planData, trackPoints, out alarmCounters, out alarms, out messages, out _);
        }

        internal static RideStats Ride(Preferences prefs, TimeSpan? playDuration,
            IPlanData planData,
            IReadOnlyList<GpsPoint> trackPoints,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages,
            out TurnLookout lookout)
        {
            return RideWithGps(prefs, playDuration,
                planData,
                trackPoints.Select(it => (GpsPoint?)it).ToList(),
                out alarmCounters,
                out alarms,
                out messages,
                out lookout);
        }

        internal static RideStats RideWithGps(Preferences prefs, TimeSpan? playDuration,
            IPlanData planData,
            IReadOnlyList<GpsPoint?> trackPoints,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages,
            out TurnLookout lookout)
        {


            var speeds = new List<Speed>();
            var clock = new SecondStamper();
            using (var raw_alarm_master = new AlarmMaster(clock))
            {
                raw_alarm_master.PopulateAlarms(playDuration);

                var service = new TrackRadar.Tests.Implementation.MockRadarService(prefs, clock);
                var signal_service = new ManualSignalService(clock);

                // using (var watchdog = new GpsWatchdog(signal_service, clock, prefs.GpsAcquisitionTimeout,
                //   prefs.GpsLossTimeout, noGpsAgainInterval: prefs.NoGpsAlarmAgainInterval))

                // watchdog.Start();

                ILogger logger = service;
                var counting_alarm_master = new CountingAlarmMaster(logger, raw_alarm_master);

                AlarmSequencer sequencer = new AlarmSequencer(service, counting_alarm_master);
                using (var core = new RadarCore(service, signal_service, new GpsAlarm(sequencer), sequencer, clock, planData, Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero))
                {
                    core.SetupGpsWatchdog(prefs);

                    lookout = core.Lookout;

                    int point_index = 0;
                    long longest_update = 0;
                    long start_all = Stopwatch.GetTimestamp();
                    foreach (GpsPoint? pt in trackPoints)
                    {
                        using (sequencer.OpenAlarmContext(gpsAcquired: false, hasGpsSignal: true))
                        {
                            if (point_index == 44)
                            {
                                ;
                            }
                            counting_alarm_master.SetPointIndex(point_index);
                            long start = Stopwatch.GetTimestamp();
                            if (pt.HasValue)
                                core.UpdateLocation(pt.Value.Point, null, accuracy: pt.Value.Accuracy);
                            speeds.Add(core.RidingSpeed);
                            long passed = Stopwatch.GetTimestamp() - start;
                            if (longest_update < passed)
                                longest_update = passed;
                            clock.Advance();

                            //  signal_service.Timer.TriggerCallback();

                            ++point_index;
                        }
                    }


                    alarmCounters = counting_alarm_master.AlarmCounters;
                    alarms = counting_alarm_master.Alarms;
                    messages = counting_alarm_master.Messages;

                    return new RideStats(planData, trackPoints, speeds, longest_update * 1.0 / Stopwatch.Frequency,
                        (Stopwatch.GetTimestamp() - start_all - 0.0) / (Stopwatch.Frequency * trackPoints.Count),
                        trackPoints.Count,
                        alarmCounters, alarms, messages);
                }
            }

        }

        public static void PrintSpeeds(IEnumerable<Speed> speeds)
        {
            foreach (var sp in speeds)
                Console.WriteLine(sp.KilometersPerHour.ToString("0.##"));
        }

        public static Preferences CreatePreferences()
        {
            return new Preferences()
            {
                TurnAheadAlarmDistance = TimeSpan.FromSeconds(17),
                GpsFilter = true
            };
        }

        internal static Preferences LowThresholdSpeedPreferences()
        {
            var prefs = CreatePreferences();
            prefs.RestSpeedThreshold = Speed.Zero;
            prefs.RidingSpeedThreshold = Speed.FromMetersPerSecond(1);
            return prefs;
        }

#if DEBUG
        public static void SaveGpxSegments(string filename, IEnumerable<ISegment> segments)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                int idx = 0;
                foreach (ISegment seg in segments)
                {
                    writer.WriteTrack(seg.Points().ToArray(), $"{idx}:{seg.SectionId}");
                    ++idx;
                }
            }
        }
        public static void SaveGpx(string filename, IPlanData plan)
        {
            using (GpxDirtyWriter.Create(filename, out IGpxDirtyWriter writer))
            {
                {
                    int idx = 0;
                    foreach (ISegment seg in plan.Segments)
                    {
                        writer.WriteTrack(seg.Points().ToArray(), $"Line {idx}:{seg.SectionId} #{seg.__DEBUG_id}");
                        ++idx;
                    }
                }
                {
                    foreach (var cx_entry in plan.Crossroads)
                    {
                        writer.WriteWaypoint(cx_entry.Key, $"Point {cx_entry.Value}");
                    }
                }
            }
        }
#endif

        public static IReadOnlyList<GeoPoint> GetCrossroadsList(this IPlanData plan)
        {
            return plan.Crossroads.OrderBy(it => it.Value).Select(it => it.Key).ToList();
        }
        public static void SaveGpx(string filename, IEnumerable<IEnumerable<GeoPoint>> segments,
            IEnumerable<GeoPoint> waypoints)
        {
#if DEBUG
            GpxToolbox.SaveGpx(filename, segments, waypoints);
#endif
        }
        public static void SaveGpxSegments(string filename, params IEnumerable<GeoPoint>[] segments)
        {
#if DEBUG
            GpxToolbox.SaveGpxSegments(filename, segments);
#endif
        }
        public static void SaveGpxAlarms(string filename, RideStats stats)
        {
#if DEBUG
            GpxToolbox.SaveGpxWaypoints(filename, stats.Alarms
                .Where(a => stats.TrackPoints[a.index].HasValue)
                .Select(a => (stats.TrackPoints[a.index].Value, $"{a.index}. {a.alarm}")));
#endif
        }
        public static void SaveGpxWaypoints(string filename, IEnumerable<GpsPoint> points)
        {
#if DEBUG
            GpxToolbox.SaveGpxWaypoints(filename, points);
#endif
        }

        internal static void PopulateAlarms(this AlarmMaster alarmMaster, TimeSpan? duration = null)
        {
            alarmMaster.Reset(new TestAlarmVibrator(),
                offTrackPlayer: new TestAlarmPlayer(AlarmSound.OffTrack, duration),
                gpsLostPlayer: new TestAlarmPlayer(AlarmSound.GpsLost, duration),
                gpsOnPlayer: new TestAlarmPlayer(AlarmSound.BackOnTrack, duration),
                disengage: new TestAlarmPlayer(AlarmSound.Disengage, duration),
                crossroadsPlayer: new TestAlarmPlayer(AlarmSound.Crossroad, duration),
                goAhead: new TestAlarmPlayer(AlarmSound.GoAhead, duration),
                leftEasy: new TestAlarmPlayer(AlarmSound.LeftEasy, duration),
                leftCross: new TestAlarmPlayer(AlarmSound.LeftCross, duration),
                leftSharp: new TestAlarmPlayer(AlarmSound.LeftSharp, duration),
                rightEasy: new TestAlarmPlayer(AlarmSound.RightEasy, duration),
                rightCross: new TestAlarmPlayer(AlarmSound.RightCross, duration),
                rightSharp: new TestAlarmPlayer(AlarmSound.RightSharp, duration),
                doubleTurn: new TestAlarmPlayer(AlarmSound.DoubleTurn, duration));
        }

        internal static List<GpsPoint> PopulateTrackDensely(IEnumerable<GpsPoint> trackPoints)
        {
            return PopulateTrackDensely(trackPoints, Speed.FromMetersPerSecond(3));
        }
        internal static List<GpsPoint> PopulateTrackDensely(IEnumerable<GeoPoint> trackPoints)
        {
            return PopulateTrackDensely(trackPoints, Speed.FromMetersPerSecond(3));
        }

        internal static List<GpsPoint> PopulateTrackDensely(IEnumerable<GeoPoint> trackPoints, Speed speed)
        {
            return PopulateTrackDensely(trackPoints.Select(pt => new GpsPoint(pt, null)).ToList(), speed);
        }
        internal static List<GpsPoint> PopulateTrackDensely(IEnumerable<GpsPoint> trackPoints, Speed speed)
        {
            var result = trackPoints.ToList();

            for (int i = 0; i < result.Count - 1; ++i)
            {
                while (GeoCalculator.GetDistance(result[i].Point, result[i + 1].Point).Meters > speed.MetersPerSecond)
                {
                    GeoPoint pt = GeoCalculator.GetMidPoint(result[i].Point, result[i + 1].Point);
                    result.Insert(i + 1, new GpsPoint(pt, (result[i].Accuracy + result[i + 1].Accuracy) / 2));
                }
            }

            return result;
        }

        internal static IEnumerable<ProximityTrackPoint> ReadTrackGpxPoints(string rideFilename)
        {
            var track_points = new List<ProximityTrackPoint>();
            using (Gpx.GpxIOFactory.CreateReader(rideFilename, new ProximityTrackPointReader(), out IGpxReader reader, out _))
            {
                while (reader.Read(out GpxObjectType type))
                {
                    if (type == GpxObjectType.Track)
                    {
                        track_points.AddRange(reader.Track.Segments.SelectMany(it => it.TrackPoints.Select(pt => pt as ProximityTrackPoint)));
                    }
                }

            }

            return track_points;
        }

        public static IPlanData CreateBasicTrackData(IEnumerable<GeoPoint> track,
            IEnumerable<GeoPoint> waypoints, Length offTrackDistance)
        {
            return CreateBasicTrackData(waypoints, offTrackDistance, track);
        }
        public static IPlanData CreateTrackData(IEnumerable<GeoPoint> track, IEnumerable<GeoPoint> waypoints, IEnumerable<GeoPoint> endpoints,
            Length offTrackDistance)
        {
            return CreateRichTrackData(waypoints, endpoints: endpoints, offTrackDistance, track);
        }
        public static IPlanData CreateBasicTrackData(IEnumerable<GeoPoint> waypoints, Length offTrackDistance,
            params IEnumerable<GeoPoint>[] tracks)
        {
            return CreateRichTrackData(waypoints, endpoints: null, offTrackDistance, tracks);
        }

        public static bool TryLoadGpx(string filename, out List<List<GeoPoint>> tracks,
         out List<GeoPoint> waypoints,
         Action<double> onProgress,
         CancellationToken token)
        {
#if DEBUG
            return GpxLoader.TryLoadGpx(filename, out tracks, out waypoints, onProgress, token);
#else
            throw new NotSupportedException();
#endif
        }

        public static IPlanData ProcessTrackData(IEnumerable<IEnumerable<GeoPoint>> tracks,
    IEnumerable<GeoPoint> waypoints, IEnumerable<GeoPoint> endpoints,
    Length offTrackDistance, Length segmentLengthLimit, Action<Stage, double> onProgress, CancellationToken token)
        {
#if DEBUG
            return GpxLoader.ProcessTrackData(MetaLogger.None, tracks, waypoints, endpoints, offTrackDistance, segmentLengthLimit, onProgress, token);
#else
            throw new NotSupportedException();
#endif
        }
        public static IPlanData ProcessTrackData(IEnumerable<IEnumerable<GeoPoint>> tracks,
    IEnumerable<GeoPoint> waypoints,
    Length offTrackDistance, Length segmentLengthLimit, Action<Stage, double> onProgress, CancellationToken token)
        {
            return ProcessTrackData(tracks, waypoints, Enumerable.Empty<GeoPoint>(), offTrackDistance, segmentLengthLimit, onProgress, token);
        }

        public static IPlanData CreateRichTrackData(IEnumerable<GeoPoint> waypoints, IEnumerable<GeoPoint> endpoints,
            Length offTrackDistance, params IEnumerable<GeoPoint>[] tracks)
        {
            return ProcessTrackData(tracks, waypoints ?? Enumerable.Empty<GeoPoint>(),
                 endpoints ?? Enumerable.Empty<GeoPoint>(),
                offTrackDistance: offTrackDistance,
                segmentLengthLimit: GeoMapFactory.SegmentLengthLimit, null, CancellationToken.None);
        }

        public static IGeoMap CreateTrackMap(IEnumerable<GeoPoint> track, params GeoPoint[] waypoints)
        {
            var prefs = Toolbox.CreatePreferences();
            IPlanData track_data = CreateBasicTrackData(track, waypoints: waypoints, prefs.OffTrackAlarmDistance);
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