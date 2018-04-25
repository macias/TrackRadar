using System;
using System.IO;
using Android.Content;

namespace TrackRadar
{
    // the file will be located in directory 
    // Android/data/TrackRadar.TrackRadar/
    public sealed class LogFile : IDisposable
    {
        private readonly StreamWriter writer;
        public string Path { get; }

        public LogFile(ContextWrapper ctx, string log_filename, DateTime expires)
        {
            bool append;
            using (var file = new Java.IO.File(ctx.GetExternalFilesDir(null), log_filename))
            //            using (var file = ctx.GetFileStreamPath(log_filename))
            {
                Path = file.AbsolutePath;
                append = file != null && file.Exists()
                    // check if last modification to file was after experition
                    && Common.FromTimeStampMs(file.LastModified()) > expires;
            }
            this.writer = new StreamWriter(Path, append);
            //            this.writer = new StreamWriter(ctx.OpenFileOutput(log_filename, FileCreationMode.WorldReadable | (append ? FileCreationMode.Append : 0)));
        }

        public void Dispose()
        {
            this.writer.Dispose();
        }

        internal void WriteLine(LogLevel level, string message)
        {
            this.writer.WriteLine($"[{Common.FormatLongDateTime(DateTimeOffset.Now)}] {level}: {message}");
            this.writer.Flush();
        }
    }

}