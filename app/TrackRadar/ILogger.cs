namespace TrackRadar
{
    public interface ILogger
    {
        void LogDebug(LogLevel level, string message);
    }   

    public static class LoggerExtension
    {
        public static void Info(this ILogger logger, string message)
        {
            logger.LogDebug(LogLevel.Info, message);
        }
        public static void Error(this ILogger logger, string message)
        {
            logger.LogDebug(LogLevel.Error, message);
        }
        public static void Verbose(this ILogger logger, string message)
        {
            logger.LogDebug(LogLevel.Verbose, message);
        }
        public static void Warning(this ILogger logger, string message)
        {
            logger.LogDebug(LogLevel.Warning, message);
        }
    }
}