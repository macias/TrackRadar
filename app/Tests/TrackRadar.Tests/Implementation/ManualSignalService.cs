using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class GpsAlarm : IGpsAlarm
    {
        private readonly IAlarmSequencer alarmSequencer;

        public int GpsOffAlarmCounter { get; private set; }

        public GpsAlarm(IAlarmSequencer alarmSequencer)
        {
            this.alarmSequencer = alarmSequencer;
        }

        bool IGpsAlarm.GpsOffAlarm(string message)
        {
            if (alarmSequencer != null && !alarmSequencer.TryAlarm(Alarm.GpsLost, false, out _))
                return false;

            ++GpsOffAlarmCounter;
            return true;
        }
    }

    internal sealed class ManualSignalService : TrackRadar.Implementation.ISignalCheckerService
    {
        private readonly SecondStamper stamper;

        public ManualTimer Timer { get; private set; }

        public ManualSignalService(SecondStamper stamper)
        {
            this.stamper = stamper;
        }

        ITimer ISignalCheckerService.CreateTimer(Action callback)
        {
            this.Timer = new ManualTimer(callback, stamper);
            return Timer;
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
