namespace TrackRadar
{
    public sealed class NoneLogger : ILogger
    {
        public static ILogger Instance { get; } = new NoneLogger();

        private NoneLogger()
        {

        }

        void ILogger.LogDebug(LogLevel level, string message)
        {
        }
    }   
}