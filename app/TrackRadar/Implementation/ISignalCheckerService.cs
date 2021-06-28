using System;

namespace TrackRadar.Implementation
{
    internal interface ISignalCheckerService
    {
        ITimer CreateTimer(Action callback);
        bool GpsOffAlarm(string message);
        void AcquireGps();
        void Log(LogLevel level, string message);
    }
}