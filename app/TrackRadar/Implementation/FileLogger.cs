using System;
using System.IO;

namespace TrackRadar.Implementation
{
    public sealed class FileLogger : ILogger
    {
        private readonly object threadLock = new object();
        private readonly StreamWriter stream;

        public FileLogger(StreamWriter stream)
        {
            this.stream = stream;
        }

        public void LogDebug(LogLevel level, string message)
        {
            lock (this.threadLock)
            {
                this.stream.WriteLine($"[{Common.FormatLongDateTime(DateTimeOffset.Now)}] {level}: {message}");
                this.stream.Flush();
            }
        }
    }

}