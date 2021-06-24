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
            const int bucket_size = 3;
            var service = new ManualSignalService(noGpsFirstTimeout: TimeSpan.FromSeconds(bucket_size),
                noGpsAgainInterval: TimeSpan.FromSeconds(bucket_size));

            var watchdog = new GpsWatchdog(service, stamper);

            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            for (int i = 0; i < bucket_size; ++i)
            {
                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(0, service.GpsOffAlarmCounter);
            }

            for (int i = 0; i < bucket_size+1; ++i)
            {
                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(1, service.GpsOffAlarmCounter);
            }

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            stamper.Advance();
            for (int i= 0;i<bucket_size - 1;++i)
                Assert.IsFalse(watchdog.UpdateGpsIsOn());

            Assert.IsTrue(watchdog.UpdateGpsIsOn());

            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            service.Timer.TriggerCallback();
            Assert.AreEqual(2, service.GpsOffAlarmCounter);
        }

        [TestMethod]
        public void UnstableAcquisitionTest()
        {
            // originally if we got signal sporadically we didn't get any alarm, because
            // no-signal resetted info about signal, and getting signal resetted info about no-signal state

            var stamper = new SecondStamper();
            const int bucket_size = 4;
            var service = new ManualSignalService(noGpsFirstTimeout: TimeSpan.FromSeconds(bucket_size),
                noGpsAgainInterval: TimeSpan.FromSeconds(50));

            var watchdog = new GpsWatchdog(service, stamper);

            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            Assert.IsFalse(watchdog.UpdateGpsIsOn());

            // now we simulate it was signal "by accident"

            for (int i = 0; i < bucket_size-2; ++i)
            {
                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(0, service.GpsOffAlarmCounter);
            }

            // this signal should not reset no-gps state completely, because we had a huge gap in getting signal

            Assert.IsFalse(watchdog.UpdateGpsIsOn());

            for (int i = 0; i < bucket_size - 2; ++i)
            {
                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(0, service.GpsOffAlarmCounter);
            }

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            // now we simulate getting stable signal

            for (int i = 0; i < bucket_size - 1; ++i)
            {
                Assert.IsFalse(watchdog.UpdateGpsIsOn());

                stamper.Advance();
                service.Timer.TriggerCallback();
                Assert.AreEqual(1, service.GpsOffAlarmCounter);
            }

            // at this point the signal is stable, so we should acquisition = true
            Assert.IsTrue(watchdog.UpdateGpsIsOn());

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);
        }
    }
}