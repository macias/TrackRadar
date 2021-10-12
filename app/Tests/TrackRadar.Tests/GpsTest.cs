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
    public class GpsTest
    {
        [TestMethod]
        public void AcquiringSignalWhenOffTrackTest()
        {
            // we have very long segment, and 3 turnings points. The purpose of the test is to check if we get
            // notification for the "middle" turn point which is far from segment points (but it lies on the segment)

            var prefs = Toolbox.LowThresholdSpeedPreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            var plan_points = new[] { GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(40.1, 5) };

            IPlanData gpx_data = Toolbox.CreateBasicTrackData(track: plan_points, waypoints: null, prefs.OffTrackAlarmDistance);

            // we simulate riding off track
            var track_points = Toolbox.PopulateTrackDensely(
                new[] { GeoPoint.FromDegrees(50, 5), GeoPoint.FromDegrees(50, 5.002) })
                .Select(it => (GpsPoint?)it)
                .ToList();

            track_points.InsertRange(0, Enumerable.Range(0, (int)(prefs.GpsAcquisitionTimeout.TotalSeconds * 2))
                .Select(_ => (GpsPoint?)null));

            var stats = Toolbox.Ride(new RideParams(prefs)
            {
                PlanData = gpx_data,
                Trace = track_points,
            });

            Assert.AreEqual(5, stats.Alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.GpsLost, 3), stats.Alarms[a++]);
            
            // off-track is our engaging alarm in such case
            Assert.AreEqual((Alarm.OffTrack, 13), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 23), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 33), stats.Alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 43), stats.Alarms[a++]);
        }

        [TestMethod]
        public void GpsWatchdogTest()
        {
            //var stamper = new ClockStamper(DateTimeOffset.UtcNow);
            var stamper = new SecondStamper();
            const int acquisision = 3;
            const int interval = 3;
            const int loss = 2;
            var signal_service = new ManualSignalService(stamper);
            var alarm = new GpsAlarm(null);

            using (var watchdog = new GpsWatchdog(signal_service, alarm, stamper, TimeSpan.FromSeconds(acquisision),
                TimeSpan.FromSeconds(loss),
                noGpsAgainInterval: TimeSpan.FromSeconds(interval)))
            {
                watchdog.Start();

                // signal_service.Timer.TriggerCallback();
                //@@@              Assert.AreEqual(0, alarm.GpsOffAlarmCounter);

                for (int i = 0; i < loss; ++i)
                {
                    stamper.Advance();
                    //   signal_service.Timer.TriggerCallback();
                    Assert.AreEqual(0, alarm.GpsOffAlarmCounter);
                }

                for (int i = 0; i < interval + 1; ++i)
                {
                    stamper.Advance();
                    // signal_service.Timer.TriggerCallback();
                    Assert.AreEqual(1, alarm.GpsOffAlarmCounter);
                }

                stamper.Advance();
                //signal_service.Timer.TriggerCallback();
                Assert.AreEqual(2, alarm.GpsOffAlarmCounter);

                Assert.IsFalse(watchdog.UpdateGpsIsOn());
                for (int i = 0; i < acquisision - 1; ++i) // first update basically sets the start of acquisition, all next increase counter
                {
                    stamper.Advance();
                    Assert.AreEqual(2, alarm.GpsOffAlarmCounter);
                    Assert.IsFalse(watchdog.UpdateGpsIsOn());
                }

                stamper.Advance();
                Assert.AreEqual(2, alarm.GpsOffAlarmCounter); //!! 3
                Assert.IsTrue(watchdog.UpdateGpsIsOn());

                stamper.Advance();
                //signal_service.Timer.TriggerCallback();
                Assert.AreEqual(2, alarm.GpsOffAlarmCounter);
            }
        }

        [TestMethod]
        public void UnstableAcquisitionTest()
        {
            // originally if we got signal sporadically we didn't get any alarm, because
            // no-signal resetted info about signal, and getting signal resetted info about no-signal state

            var stamper = new SecondStamper();
            const int acquisition = 4;
            const int loss = 2;
            var signal_service = new ManualSignalService(stamper);
            var alarm = new GpsAlarm(null);

            using (var watchdog = new GpsWatchdog(signal_service, alarm, stamper, TimeSpan.FromSeconds(acquisition),
                TimeSpan.FromSeconds(loss),
                noGpsAgainInterval: TimeSpan.FromSeconds(50)))
            {
                watchdog.Start();

                //signal_service.Timer.TriggerCallback();
                Assert.AreEqual(0, alarm.GpsOffAlarmCounter);

                stamper.Advance();
                Assert.IsFalse(watchdog.UpdateGpsIsOn());

                // now we simulate it was signal "by accident"

                for (int i = 0; i < loss - 1; ++i)
                {
                    stamper.Advance();
                    //  signal_service.Timer.TriggerCallback();
                    Assert.AreEqual(0, alarm.GpsOffAlarmCounter);
                }

                stamper.Advance();
                Assert.IsFalse(watchdog.UpdateGpsIsOn());

                stamper.Advance();
                //signal_service.Timer.TriggerCallback();
                // the gap was not lost by gps update, so finally we triggered gps-off alarm
                Assert.AreEqual(1, alarm.GpsOffAlarmCounter);

                // now we simulate getting stable signal

                for (int i = 0; i < acquisition - 1; ++i)
                {
                    stamper.Advance();
                    Assert.IsFalse(watchdog.UpdateGpsIsOn());

                    //  signal_service.Timer.TriggerCallback();
                    Assert.AreEqual(1, alarm.GpsOffAlarmCounter);
                }

                // at this point the signal is stable, so we should acquisition = true
                stamper.Advance();
                Assert.IsTrue(watchdog.UpdateGpsIsOn());

                //signal_service.Timer.TriggerCallback();
                Assert.AreEqual(1, alarm.GpsOffAlarmCounter);
            }
        }
    }
}