using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    public enum AlarmMode
    {
        Enqueue,
    }
    
    internal sealed class ServiceAlarms : IDisposable
    {
        private readonly object threadLock = new object();

        private IAlarmVibrator vibrator;
        private readonly IReadOnlyList<Alarm> alarms;
        private readonly IAlarmPlayer[] players;
        private readonly long[] playStartedAt;
        private readonly long[] playStoppedAt;
        private readonly ITimeStamper stamper;
        private readonly IReadOnlyList<Alarm> turnAheads;

        public ServiceAlarms(ITimeStamper stamper)
        {
            this.stamper = stamper;
            this.alarms = LinqExtension.GetEnums<Alarm>().ToList();
            this.players = new IAlarmPlayer[alarms.Count];
            this.playStartedAt = new long[alarms.Count];
            this.playStoppedAt = new long[alarms.Count];
            for (int i=0;i<alarms.Count;++i)
            {
                this.playStartedAt[i] = stamper.GetBeforeTimeTimestamp();
                this.playStoppedAt[i] = stamper.GetBeforeTimeTimestamp();
            }
            this.turnAheads = new Alarm[] {
                Alarm.Crossroad,
                Alarm.GoAhead,
                Alarm.LeftEasy,
                Alarm.LeftCross,
                Alarm.LeftSharp,
                Alarm.RightEasy,
                Alarm.RightCross,
                Alarm.RightSharp,
           };
        }

        public void Dispose()
        {
            lock (this.threadLock)
                destroyPlayers();
        }

        private void destroyPlayers()
        {
            this.vibrator?.Dispose();
            this.vibrator = null;
            for (int i = 0; i < this.players.Length; ++i)
            {
                if (this.players[i] != null)
                {
                    this.players[i].Completion -= ServiceAlarms_Completion;
                    this.players[i] = Common.DestroyMediaPlayer(this.players[i]);
                }
            }
        }

        internal void Reset(IAlarmVibrator vibrator,
           IAlarmPlayer distancePlayer,
           IAlarmPlayer gpsLostPlayer,
           IAlarmPlayer gpsOnPlayer,
           IAlarmPlayer disengage,
           IAlarmPlayer crossroadsPlayer,
           IAlarmPlayer goAhead,
           IAlarmPlayer leftEasy,
           IAlarmPlayer leftCross,
           IAlarmPlayer leftSharp,
           IAlarmPlayer rightEasy,
           IAlarmPlayer rightCross,
           IAlarmPlayer rightSharp)
        {
            lock (this.threadLock)
            {
                destroyPlayers();

                this.vibrator = vibrator;
                this.players[(int)Alarm.OffTrack] = distancePlayer;
                this.players[(int)Alarm.GpsLost] = gpsLostPlayer;
                this.players[(int)Alarm.PositiveAcknowledgement] = gpsOnPlayer;
                this.players[(int)Alarm.Disengage] = disengage;
                this.players[(int)Alarm.Crossroad] = crossroadsPlayer;

                this.players[(int)Alarm.GoAhead] = goAhead;
                this.players[(int)Alarm.LeftEasy] = leftEasy;
                this.players[(int)Alarm.LeftCross] = leftCross;
                this.players[(int)Alarm.LeftSharp] = leftSharp;
                this.players[(int)Alarm.RightEasy] = rightEasy;
                this.players[(int)Alarm.RightCross] = rightCross;
                this.players[(int)Alarm.RightSharp] = rightSharp;

                for (int i = 0; i < this.players.Count(); ++i)
                    this.players[i].Completion += ServiceAlarms_Completion;
            }
        }

        private void ServiceAlarms_Completion(object sender, EventArgs e)
        {
            lock (this.threadLock)
            {
                var mp = (IAlarmPlayer)sender;
                this.playStoppedAt[(int)(mp.Alarm)] = this.stamper.GetTimestamp();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeStamp">timestamp of the latest stopped player</param>
        /// <returns>false if some player still plays, true otherwise</returns>
        internal bool TryGetLatestTurnAheadAlarmAt(out long timeStamp)
        {
            lock (this.threadLock)
            {
                timeStamp = stamper.GetBeforeTimeTimestamp();

                if (this.turnAheads.Any(x => this.players[(int)x].IsPlaying))
                    return false;

                foreach (Alarm alarm in this.turnAheads)
                {
                    int index = (int)alarm;

                    // sanity-crazy check, but I don't trust Android, 
                    // so we fix here case when we started alarm but it didn't complete
                    if (this.playStoppedAt[index] < this.playStartedAt[index])
                    {
                        timeStamp = this.stamper.GetTimestamp();
                        this.playStoppedAt[index] = timeStamp;
                        return true;
                    }

                    timeStamp = Math.Max(timeStamp, this.playStoppedAt[index]);
                }

                return true;
            }
        }

        internal bool TryPlay(Alarm alarm, out string reason)
        {
            lock (this.threadLock)
            {
                if (alarm == Alarm.GpsLost || alarm == Alarm.OffTrack)
                    Common.VibrateAlarm(this.vibrator);

                // https://developer.android.com/reference/android/media/MediaPlayer.html

                {
                    Option<Alarm> playing = this.alarms.FirstOrNone(a =>
                    {
                        IAlarmPlayer p = this.players[(int)a];
                        return p != null && p.IsPlaying;
                    });

                    if (playing.HasValue)
                    {
                        reason = $"Cannot play {alarm}, {playing.Value} is already playing for {TimeSpan.FromSeconds(stamper.GetSecondsSpan(this.playStartedAt[(int)(playing.Value)]))}";
                        return false;
                    }
                }

                IAlarmPlayer selected_player = this.players[(int)alarm];
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
}