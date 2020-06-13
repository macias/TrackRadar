using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Android.Media;
using Android.OS;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public sealed class ServiceAlarms : IDisposable
    {
        Vibrator vibrator;
        private readonly IReadOnlyList<Alarm> alarms;
        private readonly MediaPlayer[] players;
        private readonly long[] playStartedAt;
        private readonly ITimeStamper stamper;

        public ServiceAlarms(ITimeStamper stamper)
        {
            this.alarms = LinqExtension.GetEnums<Alarm>().ToList();
            this.players = new MediaPlayer[alarms.Count];
            this.playStartedAt = new long[alarms.Count];
            this.stamper = stamper;
        }

        public void Dispose()
        {
            DestroyPlayers();
        }

        private void DestroyPlayers()
        {
            this.vibrator?.Dispose();
            this.vibrator = null;
            for (int i = 0; i < this.players.Length; ++i)
                this.players[i] = Common.DestroyMediaPlayer(this.players[i]);
        }

        internal void Reset(Vibrator vibrator,
            MediaPlayer distancePlayer,
            MediaPlayer gpsLostPlayer,
            MediaPlayer gpsOnPlayer,
            MediaPlayer crossroadsPlayer)
        {
            DestroyPlayers();

            this.vibrator = vibrator;
            this.players[(int)Alarm.OffTrack] = distancePlayer;
            this.players[(int)Alarm.GpsLost] = gpsLostPlayer;
            this.players[(int)Alarm.PositiveAcknowledgement] = gpsOnPlayer;
            this.players[(int)Alarm.Crossroad] = crossroadsPlayer;
        }

        internal bool TryPlay(Alarm alarm, out string reason)
        {
            Common.VibrateAlarm(this.vibrator);

            // https://developer.android.com/reference/android/media/MediaPlayer.html

            {
                Option<Alarm> playing = this.alarms.FirstOrNone(a =>
                {
                    MediaPlayer p = this.players[(int)a];
                    return p != null && p.IsPlaying;
                });

                if (playing.HasValue)
                {
                    reason = $"Cannot play {alarm}, {playing.Value} is already playing for {TimeSpan.FromSeconds(stamper.GetSecondsSpan(this.playStartedAt[(int)(playing.Value)]))}";
                    return false;
                }
            }

            MediaPlayer selected_player = this.players[(int)alarm];
            if (selected_player == null)
            {
                reason = $"No player for {alarm}";
                return false;
            }

            this.playStartedAt[(int)alarm] = stamper.GetTimestamp();
            selected_player.Start();
            reason = null;
            return true;
        }

    }
}