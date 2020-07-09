using MathUnit;
using System;

namespace TrackRadar
{
    public interface ILogger
    {
        void LogDebug(LogLevel level, string message);
    }   
}