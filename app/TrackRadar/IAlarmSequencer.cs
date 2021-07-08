using System;

namespace TrackRadar
{
    public interface IAlarmSequencer 
    {
        event AlarmHandler AlarmPlayed;
        event AlarmHandler AlarmNotified;

        TimeSpan MaxTurnDuration { get; }

        bool TryGetLatestTurnAheadAlarmAt(out long timeStamp);
        void PostMessage(string reason);
        void NotifyAlarm(Alarm alarm);
        IDisposable OpenAlarmContext(bool gpsAcquired, bool hasGpsSignal);
        bool TryAlarm(Alarm alarm, bool store, out string reason);
    }

}