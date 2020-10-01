using System;

namespace TrackRadar
{
    public interface IAlarmMaster
    {
        bool TryGetLatestTurnAheadAlarmAt(out long timeStamp);
        bool TryAlarm(Alarm alarm, out string reason);
        void PostMessage(string reason);
    }
}