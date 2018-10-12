using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Android.Media;
using Android.OS;

namespace TrackRadar
{
    public sealed class ServiceAlarms : IDisposable
    {
        Vibrator vibrator;
        private readonly MediaPlayer[] players;

        public ServiceAlarms()
        {
            this.players = new MediaPlayer[Enum.GetValues(typeof(Alarm)).Length];
        }
        internal void Reset(Vibrator vibrator,
            MediaPlayer distancePlayer,
            MediaPlayer gpsLostPlayer,
            MediaPlayer gpsOnPlayer,
            MediaPlayer crossroadsPlayer)
        {
            this.vibrator = vibrator;
            this.players[(int)Alarm.OffTrack] = distancePlayer;
            this.players[(int)Alarm.GpsLost] = gpsLostPlayer;
            this.players[(int)Alarm.PositiveAcknowledgement] = gpsOnPlayer;
            this.players[(int)Alarm.Crossroads] = crossroadsPlayer;
        }

        internal bool Go(Alarm alarm)
        {
            // https://developer.android.com/reference/android/media/MediaPlayer.html

            MediaPlayer p = this.players[(int)alarm];

            bool started = false;

            if (p != null && !this.players.Where(it => it != null).Any(it => it.IsPlaying))
            {
                p.Start();
                started = true;
            }

            Common.VibrateAlarm(this.vibrator);

            return started;
        }

        public void Dispose()
        {
            for (int i = 0; i < this.players.Length; ++i)
                this.players[i] = Common.DestroyMediaPlayer(this.players[i]);
        }

    }
}