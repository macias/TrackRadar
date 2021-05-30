using System;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ConsoleLogger : ILogger
    {
        void ILogger.LogDebug(LogLevel level, string message)
        {
            Console.WriteLine($"[{levelToString(level)}] {message}");
        }

        private static string levelToString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error: return "ERR";
                case LogLevel.Info: return "INF";
                case LogLevel.Verbose: return "VERB";
                case LogLevel.Warning: return "WARN";
            }

            throw new NotImplementedException();
        }
    }
}