using System;

namespace TrackRadar
{
    public interface IAlarmSequencer : IAlarmMaster
    {
        event AlarmHandler AlarmNotified;

        void NotifyAlarm(Alarm alarm);
    }

}