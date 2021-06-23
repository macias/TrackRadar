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
            var service = new ManualSignalService(noGpsFirstTimeout: TimeSpan.FromSeconds(1),
                noGpsAgainInterval: TimeSpan.FromSeconds(2));

            var checker = new GpsWatchdog(service, stamper);

            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            stamper.Advance();
            foreach (var _ in Enumerable.Range(0, GpsWatchdog.StableSignalAcquisitionCountLimit - 1))
                Assert.IsFalse(checker.UpdateGpsIsOn());

            Assert.IsTrue(checker.UpdateGpsIsOn());

            Assert.AreEqual(2, service.GpsOffAlarmCounter);

            service.Timer.TriggerCallback();
            Assert.AreEqual(2, service.GpsOffAlarmCounter);
        }

        [TestMethod]
        public void UnstableAcquisitionTest()
        {
            // originally if we got signal sporadically we didn't get any alarm, because
            // no-signal resetted info about signal, and getting signal resetted info about no-signal state

            GpsWatchdog.StableSignalAcquisitionCountLimit = 2;

            var stamper = new SecondStamper();
            var service = new ManualSignalService(noGpsFirstTimeout: TimeSpan.FromSeconds(4),
                noGpsAgainInterval: TimeSpan.FromSeconds(50));

            var checker = new GpsWatchdog(service, stamper);

            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            Assert.IsFalse(checker.UpdateGpsIsOn());

            // now we simulate it was signal "by accident"

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            // this signal should not reset no-gps state completely, because we had a huge gap in getting signal

            Assert.IsFalse(checker.UpdateGpsIsOn());

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(0, service.GpsOffAlarmCounter);

            stamper.Advance();
            service.Timer.TriggerCallback();
            // here originally we would not get alarm, but now we have it -- from last signal it is 3 seconds (so less
            // than timeout), but from the last STABLE one is more than timeout, thus alarm
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            // now we simulate getting stable signal

            Assert.IsFalse(checker.UpdateGpsIsOn());

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

            // at this point the signal is stable, so we should acquisition = true
            Assert.IsTrue(checker.UpdateGpsIsOn());

            stamper.Advance();
            service.Timer.TriggerCallback();
            Assert.AreEqual(1, service.GpsOffAlarmCounter);

        }
    }
}