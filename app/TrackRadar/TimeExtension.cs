using System;

namespace TrackRadar
{
    public static class TimeExtension
    {
        public static TimeSpan Min(this TimeSpan ts,TimeSpan other)
        {
            return TimeSpan.FromTicks(Math.Min( ts.Ticks, other.Ticks));
        }
        public static TimeSpan Max(this TimeSpan ts, TimeSpan other)
        {
            return TimeSpan.FromTicks(Math.Max(ts.Ticks, other.Ticks));
        }
    }
}