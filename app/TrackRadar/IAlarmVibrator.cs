using System;

namespace TrackRadar
{
    internal interface IAlarmVibrator : IDisposable
    {
        void Vibrate(TimeSpan duration);
    }
}