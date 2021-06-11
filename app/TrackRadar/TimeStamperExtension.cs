namespace TrackRadar
{
   

    public static class TimeStamperExtension
    {
        public static double GetSecondsSpan(this ITimeStamper stamper, long from)
        {
            return GetSecondsSpan(stamper, stamper.GetTimestamp(), from);
        }
        public static double GetSecondsSpan(this ITimeStamper stamper, long now, long from)
        {
            // use doubles at once, because long type does not cover long type (sic!), i.e. long.MaxValue-long.MinValue is out of range for long
            return (0.0 + now - from) / stamper.Frequency;
        }
        public static long GetBeforeTimeTimestamp(this ITimeStamper stamper)
        {
            return long.MinValue;
        }
    }
}