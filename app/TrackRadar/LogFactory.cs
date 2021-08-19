using System;
using System.IO;
using Android.Content;
using TrackRadar.Implementation;

namespace TrackRadar
{
    // the file will be located in directory 
    // Android/data/TrackRadar.TrackRadar/
    public static class LogFactory
    {
        public static IDisposable CreateFileLogger(ContextWrapper ctx, string filename, DateTimeOffset expires, out ILogger logger)
        {
            var stream = LogFactory.createStreamWriter(ctx, filename, expires, out _);
            logger = new FileLogger(stream);
            return stream;
        }


        public static IDisposable CreateGpxLogger(ContextWrapper ctx, string filename, DateTimeOffset expires, out IGpxDirtyWriter writer)
        {
            var stream_writer = LogFactory.createStreamWriter(ctx, filename, expires, out bool appended);
            var gpx_writer = new GpxDirtyWriter(stream_writer);
            if (!appended)
            {
                gpx_writer.WriteHeader();
                gpx_writer.WriteComment(" CLOSE gpx TAG MANUALLY ");
            }

            writer = gpx_writer;

            return stream_writer;
        }

        private static StreamWriter createStreamWriter(ContextWrapper ctx, string filename, DateTimeOffset expires, out bool appended)
        {
            string path;

            using (var file = new Java.IO.File(ctx.GetExternalFilesDir(null), filename))
            //            using (var file = ctx.GetFileStreamPath(log_filename))
            {
                path = file.AbsolutePath;
                // if the file exists and it is fresh, append instead of creating anew
                appended = file.Exists() && Common.FromTimeStampMs(file.LastModified()) > expires;
            }

            return new StreamWriter(path, appended);
        }
    }

}