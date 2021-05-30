namespace TrackRadar.Implementation
{
#if DEBUG
    public sealed class MetaLogger
    {
        public static MetaLogger None { get; } = new MetaLogger(NoneGpxDirtyWriter.Instance, NoneLogger.Instance);

        public IGpxDirtyWriter GpxLogger { get; }
        public ILogger TextLogger { get; }

        public MetaLogger(IGpxDirtyWriter gpxLogger, ILogger logger)
        {
            GpxLogger = gpxLogger;
            TextLogger = logger;
        }

    }
#endif

}