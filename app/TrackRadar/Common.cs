using System;
using System.Threading;
using Android.Content;
using Android.Media;
using Android.OS;

namespace TrackRadar
{
    public static class Common
    {
        private const string appTag = nameof(TrackRadar);

        public static string FormatLongDateTime(DateTimeOffset dto)
        {
            return DateTimeOffset.Now.ToString("O");
        }
        public static string FormatShortDateTime(DateTimeOffset dto)
        {
            return DateTimeOffset.Now.ToString("dd_HH:mm:ss.fff");
        }
        public static DateTimeOffset FromTimeStampMs(long timeStamp)
        {
            return new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero) + TimeSpan.FromMilliseconds(timeStamp);
        }
        public static string ThreadInfo()
        {
            return $"C{System.Threading.Thread.CurrentThread.ManagedThreadId},J{Java.Lang.Thread.CurrentThread().Id}-{Java.Lang.Thread.CurrentThread().Name}";
        }
        public static bool IsDebugMode(Context context)
        {
            // it will show messages from the service to the main screen only to likely-developers
            // so we check if the debugging is enabled
            return Android.Provider.Settings.Secure.GetInt(context.ContentResolver, Android.Provider.Settings.Secure.AdbEnabled, 0) == 1;
        }

        public static void Log(string message)
        {
            Android.Util.Log.Debug(Common.appTag, Common.FormatShortDateTime(DateTime.Now) + " " + message);
        }
        public static void VibrateAlarm(Vibrator vibrator)
        {
            vibrator.Vibrate(500); //ms
        }
        public static MediaPlayer CreateMediaPlayer(Context context, string filename, int resourceId)
        {
            if (String.IsNullOrEmpty(filename))
                return MediaPlayer.Create(context, resourceId);
            else if (System.IO.File.Exists(filename))
                return MediaPlayer.Create(context, Android.Net.Uri.FromFile(new Java.IO.File(filename)));
            else
                return null;
        }

        public static void DestroyMediaPlayer(ref MediaPlayer player)
        {
            MediaPlayer p = Interlocked.Exchange(ref player, null);
            if (p == null)
                return;

            if (p.IsPlaying)
                p.Stop();
            p.Dispose();
        }
        public static void SetVolume(MediaPlayer player, int currVolume)
        {
            if (player == null)
                return;

            int maxVolume = 100;
            float log1 = (float)(Math.Log10(maxVolume - currVolume) / Math.Log10(maxVolume));
            player.SetVolume(1 - log1, 1 - log1);
        }


    }
}