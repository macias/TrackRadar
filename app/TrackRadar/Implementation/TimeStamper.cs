namespace TrackRadar.Implementation
{
    internal sealed class TimeStamper : ITimeStamper
    {
        public long Frequency => System.Diagnostics.Stopwatch.Frequency;

        public long GetTimestamp()
        {
            return System.Diagnostics.Stopwatch.GetTimestamp();
        }
    }

}