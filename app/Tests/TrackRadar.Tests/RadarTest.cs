using Geo;
using Gpx;
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
            var track_points = new[] { GeoPoint.FromDegrees(40.05,5), GeoPoint.FromDegrees(40.05,5.01) }.ToList();
            Toolbox.PopulateTrackDensely(track_points);
            track_points.AddRange(track_points.AsEnumerable().Reverse());

            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(6, alarms.Count());

            int a = 0;
            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.OffTrack, 37), alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 47), alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 57), alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 67), alarms[a++]);

            // we don't have second disengage in the middle of riding (when turning back) because now TrackRadar checks
            // the state not the speed, and since multiple off-track causes disengage, the single alarm suffices

            Assert.AreEqual((Alarm.BackOnTrack, 989), alarms[a++]);
        }

        [TestMethod]
        public void PreferencesTrackNameTest()
        {
            var prefs = Toolbox.CreatePreferences();
            prefs.TurnAheadAlarmDistance = TimeSpan.FromSeconds(13) ;
            PropertyInfo property = prefs.GetType().GetProperty(nameof(Preferences.TrackName));
            property.SetValue(prefs, "foobar");
            Assert.AreEqual("foobar", prefs.TrackName);
            var clone = prefs.Clone();
            Assert.AreEqual("foobar", clone.TrackName);
        }
        [TestMethod]
        public void TestSignalChecker()
        {
            //var stamper = new ClockStamper(DateTimeOffset.UtcNow);
            var stamper = new SecondStamper();
            var service = new ManualSignalService(noGpsFirstTimeout: TimeSpan.FromSeconds(1),
                noGpsAgainInterval: TimeSpan.FromSeconds(2));

            var checker = new GpsWatchdog(service, stamper);

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();// TimeSpan.FromSeconds(1));
            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();// TimeSpan.FromSeconds(1));
            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            stamper.Advance();// TimeSpan.FromSeconds(1));
            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            stamper.Advance();// TimeSpan.FromSeconds(1));
            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            stamper.Advance();// TimeSpan.FromSeconds(1));
            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            stamper.Advance();// TimeSpan.FromSeconds(1));
            foreach (var _ in Enumerable.Range(0, 9)) // it takes 10 updates to switch the state
                Assert.IsFalse(checker.UpdateGpsIsOn());

            Assert.IsTrue(checker.UpdateGpsIsOn());

            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(2, service.GpsOffAlarmCounter);
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
            string plan_filename = @"Data/z-two-turns.plan.gpx";

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
            string plan_filename = @"Data/dup-turn-point.plan.gpx";
            string tracked_filename = @"Data/dup-turn-point.tracked.gpx";

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
                    out  _);
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
        [DataRow(@"Data/single-segment.gpx")]
        [DataRow(@"Data/single-point.gpx")]
        public void TestLoading(string planFilename)
        {
            var prefs = Toolbox.CreatePreferences();
            prefs.TurnAheadAlarmDistance = TimeSpan.FromSeconds(13);
            IPlanData gpx_data = Toolbox.LoadPlan(prefs, planFilename);

            //ClockStamper clock = new ClockStamper(DateTimeOffset.UtcNow);
            var clock = new SecondStamper();
            using (var raw_alarm_master = new AlarmMaster(clock))
            {
                raw_alarm_master.PopulateAlarms();

                var service = new TrackRadar.Tests.Implementation.RadarService(prefs, clock);
                var core = new RadarCore(service, new AlarmSequencer(service, raw_alarm_master), clock, gpx_data, Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero);

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
