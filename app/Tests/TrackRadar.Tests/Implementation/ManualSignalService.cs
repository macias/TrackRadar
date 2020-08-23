using Geo;
using Gpx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ManualSignalService : TrackRadar.Implementation.ISignalCheckerService
    {
        public ManualTimer Timer { get; private set; }

        public TimeSpan NoGpsFirstTimeout { get; }
        public TimeSpan NoGpsAgainInterval { get; }
        public int GpsOnAlarmCounter { get; private set; }
        public int GpsOffAlarmCounter { get; private set; }

        public ManualSignalService(TimeSpan noGpsFirstTimeout, TimeSpan noGpsAgainInterval)
        {
            NoGpsFirstTimeout = noGpsFirstTimeout;
            NoGpsAgainInterval = noGpsAgainInterval;
        }

        public ITimer CreateTimer(Action callback)
        {
            this.Timer = new ManualTimer(callback);
            return Timer;
        }

        public void GpsOnAlarm()
        {
            ++GpsOnAlarmCounter;
        }

        public void GpsOffAlarm(string _)
        {
            ++GpsOffAlarmCounter;
        }

        public void Log(LogLevel level, string message)
        {
            ;
        }
    }

}
