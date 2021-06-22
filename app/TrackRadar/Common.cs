using System;
using Android.Content;
using Android.Media;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public static class Common
    {
        private const string appTag = nameof(TrackRadar);

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
            Android.Util.Log.WriteLine(priority, Common.appTag, Formatter.FormatShortDateTime(DateTime.Now) + " " + message);
        }

        internal static void VibrateAlarm(IAlarmVibrator vibrator)
        {
            vibrator?.Vibrate(TimeSpan.FromMilliseconds(500)); //ms
        }
        public static IAlarmPlayer CreateMediaPlayer(Context context, AlarmSound alarm, string filename, int resourceId)
        {
            MediaPlayer mp = CreateMediaPlayer(context, filename, resourceId, out TimeSpan duration);
            if (mp == null)
                return null;
            else
                return new AlarmPlayer(mp, duration, alarm);
        }

        public static MediaPlayer CreateMediaPlayer(Context context, string filename, int resourceId, out TimeSpan duration)
        {
            // approach with MMR extraction didn't work for me (I got nulls as duration), but MP seems to work so...
            /*TimeSpan getDuration(MediaMetadataRetriever mmr)
            {
                string duration_str = mmr.ExtractMetadata(MetadataKey.Duration);
                if (duration_str == null)
                    return TimeSpan.FromSeconds(1.5); // hackery fallback
                else
                    return TimeSpan.FromMilliseconds(int.Parse(duration_str, CultureInfo.InvariantCulture));
            }*/

            if (String.IsNullOrEmpty(filename))
            {
                /*using (MediaMetadataRetriever mmr = new MediaMetadataRetriever())
                {
                    mmr.SetDataSource(context.Resources.OpenRawResourceFd(resourceId).FileDescriptor);
                    duration = getDuration(mmr);
                }*/

                var mp = MediaPlayer.Create(context, resourceId);
                duration = TimeSpan.FromMilliseconds(mp.Duration);
                return mp;
            }
            else if (System.IO.File.Exists(filename))
            {
                Java.IO.File file = new Java.IO.File(filename);
                /*using (MediaMetadataRetriever mmr = new MediaMetadataRetriever())
                {
                    mmr.SetDataSource(file.AbsolutePath);
                    duration = getDuration(mmr);
                }*/

                var mp = MediaPlayer.Create(context, Android.Net.Uri.FromFile(file));
                duration = TimeSpan.FromMilliseconds(mp.Duration);
                return mp;
            }
            else
            {
                duration = TimeSpan.Zero;
                return null;
            }
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