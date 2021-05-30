using System;
using System.IO;
using Android.Content;

namespace TrackRadar
{
    // the file will be located in directory 
    // Android/data/TrackRadar.TrackRadar/
    public sealed class HotWriter : IDisposable
    {
        internal static StreamWriter CreateStreamWriter(ContextWrapper ctx, string filename, DateTime expires, out bool appended)
        {
            string path;

            using (var file = new Java.IO.File(ctx.GetExternalFilesDir(null), filename))
            //            using (var file = ctx.GetFileStreamPath(log_filename))
            {
                path = file.AbsolutePath;
                appended = file != null && file.Exists()
                    // check if last modification to file was after experition
                    && Common.FromTimeStampMs(file.LastModified()) > expires;
            }

            return new StreamWriter(path, appended);
        }

        private readonly object threadLock = new object();

        private readonly StreamWriter writer;

        public HotWriter(ContextWrapper ctx, string filename, DateTime expires,out bool appended)
        {
            this.writer = CreateStreamWriter(ctx, filename, expires, out appended);
        }


        public void Dispose()
        {
            this.writer.Dispose();
        }

        public void WriteLine(string message)
        {
            lock (this.threadLock)
            {
                this.writer.WriteLine(message);
                this.writer.Flush();
            }
        }
        public void Write(string message)
        {
            lock (this.threadLock)
            {
                this.writer.Write(message);
            }
        }
    }

}