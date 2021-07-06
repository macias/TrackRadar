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
        public void GpsWatchdogTest()
        {
            //var stamper = new ClockStamper(DateTimeOffset.UtcNow);
            var stamper = new SecondStamper();
            const int acquisision = 3;
            const int loss = 2;
            var service = new ManualSignalService();

            var watchdog = new GpsWatchdog(service, stamper, TimeSpan.FromSeconds(acquisision),
                TimeSpan.FromSeconds(loss),
                noGpsAgainInterval: TimeSpan.FromSeconds(loss));
            watchdog.Start();

            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            for (int i = 0; i < loss; ++i)
            {
                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(0, service.GpsOffAlarmCounter);
            }

            for (int i = 0; i < loss + 1; ++i)
            {
                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(1, service.GpsOffAlarmCounter);
            }

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            for (int i = 0; i < acquisision; ++i) // first update basically sets the start of acquisition, all next increase counter
            {
                stamper.Advance();
                Assert.IsFalse(watchdog.UpdateGpsIsOn());
            }

            stamper.Advance();
            Assert.IsTrue(watchdog.UpdateGpsIsOn());
            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(2, service.GpsOffAlarmCounter);
        }

        [TestMethod]
        public void UnstableAcquisitionTest()
        {
            // originally if we got signal sporadically we didn't get any alarm, because
            // no-signal resetted info about signal, and getting signal resetted info about no-signal state

            var stamper = new SecondStamper();
            const int acquisition = 4;
            const int loss = 2;
            var service = new ManualSignalService();

            var watchdog = new GpsWatchdog(service, stamper, TimeSpan.FromSeconds(acquisition),
                TimeSpan.FromSeconds(loss),
                noGpsAgainInterval: TimeSpan.FromSeconds(50));
            watchdog.Start();

            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();
            Assert.IsFalse(watchdog.UpdateGpsIsOn());

            // now we simulate it was signal "by accident"

            for (int i = 0; i < loss - 1; ++i)
            {
                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(0, service.GpsOffAlarmCounter);
            }

            stamper.Advance();
            Assert.IsFalse(watchdog.UpdateGpsIsOn());

            stamper.Advance();
            service.Timer.TriggerCallback();
            // the gap was not lost by gps update, so finally we triggered gps-off alarm
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            // now we simulate getting stable signal

            for (int i = 0; i < acquisition - 1; ++i)
            {
                stamper.Advance();
                Assert.IsFalse(watchdog.UpdateGpsIsOn());

                service.Timer.TriggerCallback();
                Assert.AreEqual(1, service.GpsOffAlarmCounter);
            }

            // at this point the signal is stable, so we should acquisition = true
            stamper.Advance();
            Assert.IsTrue(watchdog.UpdateGpsIsOn());

            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);
        }
    }
}