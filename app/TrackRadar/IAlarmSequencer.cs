using System;

namespace TrackRadar
{
    public interface IAlarmSequencer : IAlarmMaster
    {
        void NotifyAlarm(Alarm alarm);
    }

}