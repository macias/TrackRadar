namespace TrackRadar
{
    public interface ITimeStamper
    {
        long Frequency { get; }

        long GetTimestamp();
    }

    public static class TimeStamperExtension
    {
        public static double GetSecondsSpan(this ITimeStamper stamper,long from)
        {
            return GetSecondsSpan(stamper, stamper.GetTimestamp(), from);
        }
        public static double GetSecondsSpan(this ITimeStamper stamper,long now, long from)
        {
            return (now - from - 0.0) / stamper.Frequency;
        }
    }
}