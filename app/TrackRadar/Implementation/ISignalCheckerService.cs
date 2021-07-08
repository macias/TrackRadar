using System;

namespace TrackRadar.Implementation
{
    internal interface IGpsAlarm
    {
        bool GpsOffAlarm(string message);
    }

    internal interface ISignalCheckerService
    {
        ITimer CreateTimer(Action callback);
        void AcquireGps();
        void Log(LogLevel level, string message);
    }
}