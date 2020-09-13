using System.Runtime.CompilerServices;
using TrackRadar.Implementation;

[assembly: InternalsVisibleTo("TrackRadar.Tests")]

namespace TrackRadar
{
    public enum Alarm
    {
        OffTrack = AlarmSound.OffTrack,
        GpsLost = AlarmSound.GpsLost,
        BackOnTrack = AlarmSound.BackOnTrack,
        Crossroad = AlarmSound.Crossroad,
        GoAhead = AlarmSound.GoAhead,
        LeftEasy = AlarmSound.LeftEasy,
        LeftCross = AlarmSound.LeftCross,
        LeftSharp = AlarmSound.LeftSharp,
        RightEasy = AlarmSound.RightEasy,
        RightCross = AlarmSound.RightCross,
        RightSharp = AlarmSound.RightSharp,

        Disengage = AlarmSound.Disengage, // WATCH OUT -- need to be the last of the AlarmSounds

        GpsAcquired,
        Engaged,
    }


    public static class AlarmExtension
    {
        public static AlarmSound GetSound(this Alarm alarm)
        {
            if (alarm == Alarm.BackOnTrack || alarm == Alarm.Engaged || alarm == Alarm.GpsAcquired)
                return AlarmSound.BackOnTrack;
            else
                return (AlarmSound)alarm;
        }

        public static Alarm ToAlarm(this TurnKind turnKind)
        {
            return (Alarm)turnKind;
        }
    }
}
