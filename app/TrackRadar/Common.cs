using System;
using System.Collections.Generic;
using System.Threading;
using Android.Content;
using Android.Media;
using Android.OS;
using Gpx;

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

        public static void Log(LogLevel level, string message)
        {
            Android.Util.LogPriority priority;
            switch (level)
            {
                case LogLevel.Error:
                    priority = Android.Util.LogPriority.Error;
                    break;
                case LogLevel.Warning:
                    priority = Android.Util.LogPriority.Warn;
                    break;
                case LogLevel.Info:
                    priority = Android.Util.LogPriority.Info;
                    break;
                case LogLevel.Verbose:
                    priority = Android.Util.LogPriority.Verbose;
                    break;
                default:
                    priority = Android.Util.LogPriority.Error;
                    break;
            }
            Android.Util.Log.WriteLine(priority, Common.appTag, Common.FormatShortDateTime(DateTime.Now) + " " + message);
        }

        internal static void VibrateAlarm(IAlarmVibrator vibrator)
        {
            vibrator?.Vibrate(TimeSpan.FromMilliseconds(500)); //ms
        }
        public static IAlarmPlayer CreateMediaPlayer(Context context, AlarmSound alarm, string filename, int resourceId)
        {
            MediaPlayer mp = CreateMediaPlayer(context, filename, resourceId);
            if (mp == null)
                return null;
            else
                return new AlarmPlayer(mp, alarm);
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

        public static IAlarmPlayer DestroyMediaPlayer(IAlarmPlayer player)
        {
            if (player == null)
                return player;

            if (player.IsPlaying)
                player.Stop();
            player.Dispose();
            return null;
        }
        public static MediaPlayer DestroyMediaPlayer(MediaPlayer player)
        {
            if (player == null)
                return player;

            if (player.IsPlaying)
                player.Stop();
            player.Dispose();
            return null;
        }
        public static void SetVolume(MediaPlayer player, int currVolume)
        {
            if (player == null)
                return;

            int maxVolume = 100;
            float log1 = (float)(Math.Log10(maxVolume - currVolume) / Math.Log10(maxVolume));
            player.SetVolume(1 - log1, 1 - log1);
        }

        /*      public static List<GpxTrackSegment> ReadGpx(string filename)
              {
                  var result = new List<GpxTrackSegment>();

                  using (var input = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                  {
                      using (GpxReader reader = new GpxReader(input))
                      {
                          while (reader.Read())
                          {
                              switch (reader.ObjectType)
                              {
                                  case GpxObjectType.Metadata:
                                      break;
                                  case GpxObjectType.WayPoint:
                                      break;
                                  case GpxObjectType.Route:
                                      break;
                                  case GpxObjectType.Track:
                                      result.AddRange(reader.Track.Segments);
                                      break;
                              }
                          }

                      }

                  }

                  return result;
              }
      */

    }
}