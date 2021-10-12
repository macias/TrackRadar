using Geo;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    [TestClass]
    public class RadarTest
    {
        private const double precision = 0.00000001;

        [TestMethod]
        public void UphillTest()
        {
            // this "test" does nothing, it is simply a reminder/example how crazy computations can be
            // On an average hill (going uphill) program computes 115% or 180% gradients because the
            // accuracy of location spikes from 4m to 16m 

            // until finding out how to cancel such effects, using elevation remains disabled

            string filename = Toolbox.TestData(@"uphill.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            RideStats stats;

            stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanFilename = filename,
                TraceFilename = filename,
                UseTraceTimestamps = true,
                ReportOnlyDuration = false,
                ExtendPlanEnds = true,
                InitMinAccuracy = Length.FromMeters(4),
                ReadAltitude = true,
            });
        }

        [TestMethod]
        public void GpsAcquiredOffTrackTest()
        {
            // the point of this test is to ensure that we won't get gps-acquired alarm when we are
            // off-track (we should get off-track alarm instead)
            string tracked_filename = Toolbox.TestData(@"gps-acquired-off-track.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            RideStats stats;
            IPlanData plan = Toolbox.ProcessTrackData(new PlanParams(prefs)
                .AddTrack(new GeoPoint(), new GeoPoint(latitude: Angle.FromDegrees(1), longitude: new Angle())), 
                CancellationToken.None);

            stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanData = plan,
                TraceFilename = tracked_filename,
                UseTraceTimestamps = true,
                ReportOnlyDuration = false,
            });

            Assert.AreEqual(5, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.GpsLost, 0), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 6), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 9), stats.Alarms[a++]);
            // it would be great to get rid of these, because there was no riding
            Assert.AreEqual((Alarm.OffTrack, 21), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 24), stats.Alarms[a++]);
        }

        [TestMethod]
        public void IMPROVE_StartAndStopTest()
        {
            // the general problem is current gps-filtering slows down dis-/engagement, however
            // even with disabling it, the improvement is around 1 second reaction time
            // the aim is to improve it much more (how -- this is the question!)

            string plan_filename = Toolbox.TestData(@"start-and-stop.gpx");
            string tracked_filename = Toolbox.TestData(@"start-and-stop.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            prefs.GpsFilter = true;
            RideStats stats;
            IPlanData plan = Toolbox.LoadPlanLogged(
#if DEBUG
                MetaLogger.None,
#endif
prefs, plan_filename, extendEnds: true);

            stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanData = plan,
                TraceFilename = tracked_filename,
                InitMinAccuracy = Length.FromMeters(8)
            });

            /* without filter

Assert.AreEqual(8, stats.Alarms.Count);
int a = 0;

Assert.AreEqual((Alarm.Engaged, 61), stats.Alarms[a++]);
Assert.AreEqual((Alarm.Disengage, 75), stats.Alarms[a++]);
Assert.AreEqual((Alarm.Engaged, 81), stats.Alarms[a++]);
Assert.AreEqual((Alarm.Disengage, 94), stats.Alarms[a++]);
Assert.AreEqual((Alarm.Engaged, 102), stats.Alarms[a++]);
Assert.AreEqual((Alarm.Disengage, 115), stats.Alarms[a++]);
Assert.AreEqual((Alarm.Engaged, 123), stats.Alarms[a++]);
Assert.AreEqual((Alarm.Disengage, 138), stats.Alarms[a++]);
              
             */
            Assert.AreEqual(8, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 62), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 75), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 81), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 94), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 105), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 115), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 123), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 138), stats.Alarms[a++]);
        }

        [TestMethod]
        public void LongStartTest()
        {
            // with gps accuracy filtering it took too long for engagement to kick in

            string plan_filename = Toolbox.TestData(@"long-start.gpx");
            string tracked_filename = Toolbox.TestData(@"long-start.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            RideStats stats;
            IPlanData plan = Toolbox.LoadPlanLogged(
#if DEBUG
                MetaLogger.None,
#endif
prefs, plan_filename, extendEnds: true);

            stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanData = plan,
                TraceFilename = tracked_filename,
                InitMinAccuracy = Length.FromMeters(4)
            });

            Assert.AreEqual(1, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 10), stats.Alarms[a++]);
        }

        [TestMethod]
        public void AccuracyDrop1Test()
        {
            string plan_filename = Toolbox.TestData(@"accuracy-drop1.plan.gpx");
            string tracked_filename = Toolbox.TestData(@"accuracy-drop1.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            RideStats stats;
            stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanFilename = plan_filename,
                TraceFilename = tracked_filename
            });

            Assert.AreEqual(2, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 18), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 89), stats.Alarms[a++]);
        }

        [TestMethod]
        public void AccuracyDrop2Test()
        {
            string plan_filename = Toolbox.TestData(@"accuracy-drop2.plan.gpx");
            string tracked_filename = Toolbox.TestData(@"accuracy-drop2.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            RideStats stats;
            stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanFilename = plan_filename,
                TraceFilename = tracked_filename,
            });

            Assert.AreEqual(2, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 58), stats.Alarms[a++]);
        }

        [TestMethod]
        public void AccuracyDrop3Test()
        {
            string plan_filename = Toolbox.TestData(@"accuracy-drop3.plan.gpx");
            string tracked_filename = Toolbox.TestData(@"accuracy-drop3.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            RideStats stats;
            stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanFilename = plan_filename,
                TraceFilename = tracked_filename,
            });
            /*
            GpxToolbox.SaveGpxWaypoints("foo3.gpx",
                Implementation.Linqer.ZipIndex(stats.TrackPoints)
                .Where(it => it.value.HasValue)
                .Select(it => (it.value.Value, $"{it.index} {it.value.Value.Accuracy}")));
                */
            Assert.AreEqual(3, stats.Alarms.Count);

            int a = 0;

            Assert.AreEqual((Alarm.RightEasy, 25), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.RightEasy, 27), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 29), stats.Alarms[a++]);
        }

        [TestMethod]
        public void IMPROVE_SpeedingSlowingTest()
        {
            // the purpose of this test is to check if we can get from engage/disengage cycle because of constant
            // speeding up and slowing down (this was initial state of the program)

            // the problem is we don't have any kind of smoothing algorithm implemented and the effect of high-speeds
            // is probably due to riding under high-voltage lines, the actual ride was with low but steady speed

            // (1) we could engage/disengage if we reach speed limit several times in a row, but we have data
            // with exactly such patterns -- several low-speeds (represented as clipped 0), then several high-speeds, and so on

            // (2) we could notify about disengaging until it is really needed (like being off-track), but we need this on turns
            // as well. It is possible to pass minimal riding speed to check if the turn is needed, but this would complicate
            // logic a lot, and besides it would not communicate clearly with user. I prefer know by heart the state of the tracking

            // in short we need to improve speed calculation and smoothing

            string filename = Toolbox.TestData("slowing-speeding.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            var stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlayDuration = TimeSpan.FromSeconds(2.229),
                PlanFilename = filename,
                TraceFilename = filename,
            });

            Assert.AreEqual(20, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 32), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 40), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 49), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 52), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 62), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 71), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 80), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 89), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 92), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 96), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 103), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 144), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 213), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 217), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 268), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 273), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 399), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 401), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 405), stats.Alarms[a++]);
        }

        [TestMethod]
        public void LimitingOffTrackAlarmsTest()
        {
            // we have very long segment, and 3 turnings points. The purpose of the test is to check if we get
            // notification for the "middle" turn point which is far from segment points (but it lies on the segment)

            var prefs = Toolbox.LowThresholdSpeedPreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            var plan_points = new[] { GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(40.1, 5) };

            IPlanData gpx_data = Toolbox.CreateBasicTrackData(track: plan_points, waypoints: null, prefs.OffTrackAlarmDistance);

            // we simulate riding off track and getting back to it
            var track_points = Toolbox.PopulateTrackDensely(new[] { GeoPoint.FromDegrees(40.05, 5), GeoPoint.FromDegrees(40.05, 5.01) });
            track_points.AddRange(track_points.AsEnumerable().Reverse());

            var stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlanData = gpx_data,
            }
            .SetTrace(track_points));

            Assert.AreEqual(10, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), stats.Alarms[a++]);

            Assert.AreEqual((Alarm.OffTrack, 29), stats.Alarms[a++]); // quick off track alarm thanks to drifting check
            Assert.AreEqual((Alarm.OffTrack, 39), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 49), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 59), stats.Alarms[a++]); // we alarmed user enough

            Assert.AreEqual((Alarm.OffTrack, 515), stats.Alarms[a++]); // something to improve, program counts 180 deg turn as stopping, so it alarms about off-track again
            Assert.AreEqual((Alarm.OffTrack, 525), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 535), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 545), stats.Alarms[a++]);

            // we don't have second disengage in the middle of riding (when turning back) because now TrackRadar checks
            // the state not the speed, and since multiple off-track causes disengage, the single alarm suffices

            Assert.AreEqual((Alarm.BackOnTrack, 989), stats.Alarms[a++]);
        }

        [TestMethod]
        public void PreferencesTrackNameTest()
        {
            var prefs = Toolbox.CreatePreferences();
            prefs.TurnAheadAlarmDistance = TimeSpan.FromSeconds(13);
            PropertyInfo property = prefs.GetType().GetProperty(nameof(Preferences.TrackName));
            property.SetValue(prefs, "foobar");
            Assert.AreEqual("foobar", prefs.TrackName);
            var clone = prefs.Clone();
            Assert.AreEqual("foobar", clone.TrackName);
        }

        //private static readonly int off_track_distance_m = 70;
        //private static readonly TimeSpan off_track_interval = TimeSpan.FromSeconds(5);

        // [TestMethod]
        /*public void TestTracking()
        {
            const string plan_filename = @"../../../../../../map-merged.gpx";
            const string ride_filename = @"../../../../../../2020-04-26.gpx";

            var prefs = new Preferences();
            GpxData gpx_data = GpxLoader.ReadGpx(plan_filename, prefs.OffTrackAlarmDistance, onError: null);

            IEnumerable<GeoPoint> track_points = Toolbox.ReadTrackPoints(ride_filename).ToArray();

            Console.WriteLine($"We have {track_points.Count()} points");

            ClockStamper clock = new ClockStamper(track_points.First().Time.Value);
            var service = new TrackRadar.Tests.Implementation.RadarService(prefs, clock);
            var core = new TrackRadar.Implementation.RadarCore(service, clock, gpx_data, Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero);

            int index = 0;
            foreach (var pt in track_points)
            {
                clock.SetTime(pt.Time.Value);
                service.SetPointIndex(index);
                core.UpdateLocation(new GeoPoint(latitude: pt.Latitude, longitude: pt.Longitude), null, 0);
                ++index;
            }

            using (var writer = System.IO.File.AppendText("dump.log"))
            {
                int i = 0;
                foreach (var x in service.Alarms)
                {
                    writer.WriteLine($"Assert.AreEqual((Alarm.{x.alarm}, {x.index}), service.Alarms.ElementAt({i}))");
                    ++i;
                }
            }
            Assert.AreEqual(620, service.Alarms.Count());
            Assert.AreEqual((Alarm.GpsLost, 0), service.Alarms.ElementAt(0));
        }
        */

#if DEBUG
        [TestMethod]
        public void TurnGraphVerificationTest()
        {
            string plan_filename = Toolbox.TestData("z-two-turns.plan.gpx");

            var prefs = Toolbox.CreatePreferences();
            IPlanData gpx_data = Toolbox.LoadPlan(prefs, plan_filename);

            int index = 0;
            foreach (ISegment seg in gpx_data.Segments)
            {
                bool exists = gpx_data.Graph.DEBUG_TrackpointExists(seg.A);
                Assert.AreEqual(exists, gpx_data.Graph.DEBUG_TrackpointExists(seg.B));
                ++index;
            }
        }
#endif

        [TestMethod]
        public void OffTrackComparison_DuplicateTurnPoint_Test()
        {
            string plan_filename = Toolbox.TestData("dup-turn-point.plan.gpx");
            string tracked_filename = Toolbox.TestData("dup-turn-point.tracked.gpx");

            var prefs = Toolbox.CreatePreferences();

            IPlanData gpx_data = Toolbox.LoadPlan(prefs, plan_filename);

            var track_points = Toolbox.ReadTrackGpxPoints(tracked_filename).Select(it => GpxHelper.FromGpx(it)).ToArray();

            compareOffTrackMethods(prefs, gpx_data, track_points);
        }

        private static void compareOffTrackMethods(Preferences prefs, IPlanData trackData, IEnumerable<GeoPoint> trackPoints)
        {
            var map = RadarCore.CreateTrackMap(trackData.Segments);

            int index = 0;
            foreach (var pt in trackPoints)
            {
                bool exact_res = map.FindClosest(pt, prefs.OffTrackAlarmDistance, out ISegment _, out Length? exact_map_dist,
                    out _);
                bool approx_res = map.IsWithinLimit(pt, prefs.OffTrackAlarmDistance, out Length? approx_map_dist);

                Assert.AreEqual(expected: approx_res, actual: exact_res);
                TestHelper.IsGreaterEqual(approx_map_dist.Value, exact_map_dist.Value);

                ++index;
            }
        }

        [TestMethod]
        public void OffTrackComparison_LeavingTurningPoint_Test()
        {
            var prefs = Toolbox.CreatePreferences();

            // L-shape, but here it is irrelevant
            const double leaving_latitude = 38;
            const double leaving_longitude_start = 6;
            const double leaving_longitude_end = 6.1;

            var span_points = new[] { GeoPoint.FromDegrees(38.1,leaving_longitude_start),
                GeoPoint.FromDegrees(leaving_latitude,leaving_longitude_start),
                GeoPoint.FromDegrees(leaving_latitude,leaving_longitude_end) };

            IEnumerable<GeoPoint> track_points;
            {
                const int parts = 1000;
                track_points = Enumerable.Range(0, parts).Select(i => GeoPoint.FromDegrees(leaving_latitude,
                     leaving_longitude_start + i * (leaving_longitude_end - leaving_longitude_start) / parts))
                     .Take(100)
                     .Skip(10)
                     .ToArray();
            }

            var gpx_data = Toolbox.CreateBasicTrackData(span_points,
                // set each in-track point as turning one
                span_points.Skip(1).SkipLast(1), prefs.OffTrackAlarmDistance);

            compareOffTrackMethods(prefs, gpx_data, track_points);
        }

        [TestMethod]
        [DataRow("single-segment.gpx")]
        [DataRow("single-point.gpx")]
        public void TestLoading(string planFilename)
        {
            planFilename = Toolbox.TestData(planFilename);

            var prefs = Toolbox.CreatePreferences();
            prefs.TurnAheadAlarmDistance = TimeSpan.FromSeconds(13);
            IPlanData gpx_data = Toolbox.LoadPlan(prefs, planFilename);

            //ClockStamper clock = new ClockStamper(DateTimeOffset.UtcNow);
            var clock = new SecondStamper();
            using (var raw_alarm_master = new AlarmMaster(clock))
            {
                raw_alarm_master.PopulateAlarms(null);

                var service = new TrackRadar.Tests.Implementation.MockRadarService(prefs, clock);
                var signal_service = new ManualSignalService(clock);
                AlarmSequencer alarm_sequencer = new AlarmSequencer(service, raw_alarm_master);
                var core = new RadarCore(service, signal_service, new GpsAlarm(alarm_sequencer), alarm_sequencer, clock, gpx_data,
                    Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero
#if DEBUG
                    , RadarCore.InitialMinAccuracy
#endif
                    );
                core.SetupGpsWatchdog(prefs);

                //clock.SetTime(DateTimeOffset.UtcNow);
                //service.SetPointIndex(0);
                core.UpdateLocation(GeoPoint.FromDegrees(latitude: 10, longitude: 10), altitude: null, accuracy: null);
                clock.Advance();
            }
        }

        [TestMethod]
        public void OrderTest()
        {
            string gpx_path = "Data/z-two-turns.plan.gpx";

            Toolbox.TryLoadGpx(gpx_path, out var tracks, out var waypoints, null, CancellationToken.None);

            var track = tracks.Single();
            // just ensuring the order of the points is the same as in the file
            Assert.AreEqual(53.0148675, track[0].Latitude.Degrees, precision);
            Assert.AreEqual(53.0205342, track[1].Latitude.Degrees, precision);
            Assert.AreEqual(53.0151644, track[2].Latitude.Degrees, precision);
            Assert.AreEqual(53.0209473, track[3].Latitude.Degrees, precision);
        }

    }
}
