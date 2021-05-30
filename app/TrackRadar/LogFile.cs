using System;
using System.IO;
using Android.Content;

namespace TrackRadar
{
    public sealed class LogFile : ILogger
    {
        public static IDisposable Create(ContextWrapper ctx, string filename, DateTime expires,out ILogger logger)
        {
            var stream = HotWriter.CreateStreamWriter(ctx, filename, expires, out _);
            logger = new LogFile(stream);
            return stream;
        }

        private readonly StreamWriter stream;

        public LogFile(StreamWriter stream)
        {
            this.stream = stream;
        }


        public void LogDebug(LogLevel level, string message)
        {
            this.stream.WriteLine($"[{Common.FormatLongDateTime(DateTimeOffset.Now)}] {level}: {message}");
            this.stream.Flush();
        }
    }

}