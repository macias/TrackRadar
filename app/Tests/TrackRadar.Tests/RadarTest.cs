using Geo;
using Gpx;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    [TestClass]
    public class RadarTest
    {
        [TestMethod]
        public void TestSignalChecker()
        {
            var stamper = new ClockStamper(DateTimeOffset.UtcNow);
            var service = new ManualSignalService(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

            var checker = new SignalChecker2(service, stamper);

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(0, service.GpsOffAlarmCounter);
            Assert.AreEqual(0, service.RequestGpsCounter);

            stamper.Advance(TimeSpan.FromSeconds(1));

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(0, service.GpsOffAlarmCounter);
            Assert.AreEqual(0, service.RequestGpsCounter);

            stamper.Advance(TimeSpan.FromSeconds(1));

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(1, service.GpsOffAlarmCounter);
            Assert.AreEqual(1, service.RequestGpsCounter);

            stamper.Advance(TimeSpan.FromSeconds(1));

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(1, service.GpsOffAlarmCounter);
            Assert.AreEqual(2, service.RequestGpsCounter);

            stamper.Advance(TimeSpan.FromSeconds(1));

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(1, service.GpsOffAlarmCounter);
            Assert.AreEqual(3, service.RequestGpsCounter);

            stamper.Advance(TimeSpan.FromSeconds(1));

            service.Timer.Trigger();
            Assert.AreEqual(0, service.GpsOnAlarmCounter);
            Assert.AreEqual(2, service.GpsOffAlarmCounter);
            Assert.AreEqual(4, service.RequestGpsCounter);

            stamper.Advance(TimeSpan.FromSeconds(1));
            foreach (var _ in Enumerable.Range(0, 10)) // it takes 10 updates to switch the state
                checker.UpdateGpsIsOn(canAlarm: true);

            Assert.AreEqual(1, service.GpsOnAlarmCounter);
            Assert.AreEqual(2, service.GpsOffAlarmCounter);
            Assert.AreEqual(4, service.RequestGpsCounter);

            service.Timer.Trigger();
            Assert.AreEqual(1, service.GpsOnAlarmCounter);
            Assert.AreEqual(2, service.GpsOffAlarmCounter);
            Assert.AreEqual(4, service.RequestGpsCounter);
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
        [TestMethod]
        [DataRow(@"Data/single-segment.gpx")]
        [DataRow(@"Data/single-point.gpx")]
        public void TestLoading(string planFilename)
        {
            var prefs = new Preferences();
            GpxData gpx_data = GpxLoader.ReadGpx(planFilename, prefs.OffTrackAlarmDistance, onError: null);

            ClockStamper clock = new ClockStamper(DateTimeOffset.UtcNow);
            var service = new TrackRadar.Tests.Implementation.RadarService(prefs, clock);
            var core = new TrackRadar.Implementation.RadarCore(service, clock, gpx_data, Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero);

            clock.SetTime(DateTimeOffset.UtcNow);
            service.SetPointIndex(0);
            core.UpdateLocation(GeoPoint.FromDegrees(latitude: 10, longitude: 10), altitude: null, accuracy: 0);
        }
    }
}
