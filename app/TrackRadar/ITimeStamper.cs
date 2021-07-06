namespace TrackRadar
{
    public interface ITimeStamper
    {
        long Frequency { get; }

        long GetTimestamp();
    }
}