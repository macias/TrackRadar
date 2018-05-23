using System;
using System.IO;
using Android.Content;

namespace TrackRadar
{
    // the file will be located in directory 
    // Android/data/TrackRadar.TrackRadar/
    public sealed class HotWriter : IDisposable
    {
        private readonly StreamWriter writer;

        public HotWriter(ContextWrapper ctx, string filename, DateTime expires)
        {
            bool append;
            string path;

            using (var file = new Java.IO.File(ctx.GetExternalFilesDir(null), filename))
            //            using (var file = ctx.GetFileStreamPath(log_filename))
            {
                path = file.AbsolutePath;
                append = file != null && file.Exists()
                    // check if last modification to file was after experition
                    && Common.FromTimeStampMs(file.LastModified()) > expires;
            }
            this.writer = new StreamWriter(path, append);
            //            this.writer = new StreamWriter(ctx.OpenFileOutput(log_filename, FileCreationMode.WorldReadable | (append ? FileCreationMode.Append : 0)));
        }

        public void Dispose()
        {
            this.writer.Dispose();
        }

        internal void WriteLine(string message)
        {
            this.writer.WriteLine(message);
            this.writer.Flush();
        }
    }

}