using System;

namespace TrackRadar
{
    public delegate void AlarmHandler(object sender, Alarm alarm);

    public interface IAlarmMaster
    {
        event AlarmHandler AlarmPlayed;

        TimeSpan MaxTurnDuration { get; }

        bool TryGetLatestTurnAheadAlarmAt(out long timeStamp);
        bool TryAlarm(Alarm alarm, out string reason);
        void PostMessage(string reason);
    }
}