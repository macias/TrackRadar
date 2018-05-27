using System;
using System.IO;
using Android.Content;

namespace TrackRadar
{
    public sealed class LogFile : IDisposable
    {
        private readonly HotWriter writer;

        public LogFile(ContextWrapper ctx, string filename, DateTime expires)
        {
            this.writer = new HotWriter(ctx, filename, expires,out bool dummy);
        }

        public void Dispose()
        {
            this.writer.Dispose();
        }

        internal void WriteLine(LogLevel level, string message)
        {
            this.writer.WriteLine($"[{Common.FormatLongDateTime(DateTimeOffset.Now)}] {level}: {message}");
        }
    }

}