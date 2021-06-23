using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ManualSignalService : TrackRadar.Implementation.ISignalCheckerService
    {
        public ManualTimer Timer { get; private set; }

        public int GpsOffAlarmCounter { get; private set; }

        private readonly TimeSpan noGpsFirstTimeout;
        TimeSpan ISignalCheckerService.NoGpsFirstTimeout => this.noGpsFirstTimeout;
        private readonly TimeSpan noGpsAgainInterval;
        TimeSpan ISignalCheckerService.NoGpsAgainInterval => this.noGpsAgainInterval;

        public ManualSignalService(TimeSpan noGpsFirstTimeout, TimeSpan noGpsAgainInterval)
        {
            this.noGpsFirstTimeout = noGpsFirstTimeout;
            this.noGpsAgainInterval = noGpsAgainInterval;
        }

        ITimer ISignalCheckerService.CreateTimer(Action callback)
        {
            this.Timer = new ManualTimer(callback);
            return Timer;
        }

        bool ISignalCheckerService.GpsOffAlarm(string message)
        {
            ++GpsOffAlarmCounter;
            return true;
        }

        void ISignalCheckerService.AcquireGps()
        {
            ; // do nothing
        }

        void ISignalCheckerService.Log(LogLevel level, string message)
        {
            ; // do nothing
        }
    }

}
