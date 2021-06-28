using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ManualSignalService : TrackRadar.Implementation.ISignalCheckerService
    {
        public ManualTimer Timer { get; private set; }

        public int GpsOffAlarmCounter { get; private set; }

        public ManualSignalService()
        {
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
