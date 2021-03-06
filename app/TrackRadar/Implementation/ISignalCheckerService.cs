﻿using System;

namespace TrackRadar.Implementation
{
    internal interface ISignalCheckerService
    {
        TimeSpan NoGpsFirstTimeout { get; }
        TimeSpan NoGpsAgainInterval { get; }

        ITimer CreateTimer(Action callback);
        void GpsOffAlarm(string message);
        void Log(LogLevel level, string message);
    }
}