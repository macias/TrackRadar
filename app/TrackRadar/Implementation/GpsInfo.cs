using System;

namespace TrackRadar.Implementation
{
    internal static class GpsInfo
    {
        internal static TimeSpan WEAK_updateRate { get; } = TimeSpan.FromSeconds(1);
    }
}
