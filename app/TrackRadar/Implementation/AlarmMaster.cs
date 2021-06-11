using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("TrackRadar.Tests")]

namespace TrackRadar.Implementation
{
    public enum AlarmMode
    {
        Enqueue,
    }

    internal sealed class EnumArray<TValue>
    {
        private readonly TValue[] data;

        public IEnumerable<AlarmSound> Keys { get; }
        public TValue this[AlarmSound index] { get { return this.data[(int)index]; } set { this.data[(int)index] = value; } }

        public EnumArray()
        {
            this.Keys = LinqExtension.GetEnums<AlarmSound>().ToList();
            this.data = new TValue[Keys.Count()];
        }
        public EnumArray(TValue initValue) : this()
        {
            for (int i = 0; i < this.data.Length; ++i)
                this.data[i] = initValue;
        }

    }

    internal sealed class AlarmMaster : IDisposable, IAlarmMaster
    {
        private readonly object threadLock = new object();

        private IAlarmVibrator vibrator;
        private readonly IReadOnlyList<AlarmSound> sounds;
        private readonly EnumArray<IAlarmPlayer> players;
        private readonly EnumArray<long> playStartedAt;
        private readonly EnumArray<long> playStoppedAt;
        private readonly ITimeStamper stamper;
        private readonly IReadOnlyList<AlarmSound> turnAheads;

        public event AlarmHandler AlarmPlayed;

        public TimeSpan MaxTurnDuration { get; private set; }

        public AlarmMaster(ITimeStamper stamper)
        {
            this.stamper = stamper;
            this.sounds = LinqExtension.GetEnums<AlarmSound>().ToList();
            this.players = new EnumArray<IAlarmPlayer>();
            this.playStartedAt = new EnumArray<long>(stamper.GetBeforeTimeTimestamp());
            this.playStoppedAt = new EnumArray<long>(stamper.GetBeforeTimeTimestamp());

            this.turnAheads = new AlarmSound[] {
                AlarmSound.Crossroad,
                AlarmSound.GoAhead,
                AlarmSound.LeftEasy,
                AlarmSound.LeftCross,
                AlarmSound.LeftSharp,
                AlarmSound.RightEasy,
                AlarmSound.RightCross,
                AlarmSound.RightSharp,
                AlarmSound.DoubleTurn
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
            foreach (var i in this.players.Keys)
            {
                if (this.players[i] != null)
                {
                    this.players[i].Completion -= serviceAlarms_Completion;
                    this.players[i] = Common.DestroyMediaPlayer(this.players[i]);
                }
            }
        }

        public void Reset(IAlarmVibrator vibrator,
           IAlarmPlayer offTrackPlayer,
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
           IAlarmPlayer rightSharp,
           IAlarmPlayer doubleTurn)
        {
            lock (this.threadLock)
            {
                destroyPlayers();

                this.vibrator = vibrator;
                this.players[AlarmSound.OffTrack] = offTrackPlayer;
                this.players[AlarmSound.GpsLost] = gpsLostPlayer;
                this.players[AlarmSound.BackOnTrack] = gpsOnPlayer;
                this.players[AlarmSound.Disengage] = disengage;
                this.players[AlarmSound.Crossroad] = crossroadsPlayer;
                this.players[AlarmSound.DoubleTurn] = doubleTurn;

                this.players[AlarmSound.GoAhead] = goAhead;
                this.players[AlarmSound.LeftEasy] = leftEasy;
                this.players[AlarmSound.LeftCross] = leftCross;
                this.players[AlarmSound.LeftSharp] = leftSharp;
                this.players[AlarmSound.RightEasy] = rightEasy;
                this.players[AlarmSound.RightCross] = rightCross;
                this.players[AlarmSound.RightSharp] = rightSharp;

                foreach (var i in this.players.Keys)
                {
                    this.players[i].Completion += serviceAlarms_Completion;
                }
                this.MaxTurnDuration = TimeSpan.MinValue;
                foreach (AlarmSound turn_alarm in this.turnAheads)
                {
                    if (MaxTurnDuration < players[turn_alarm].Duration)
                        MaxTurnDuration = players[turn_alarm].Duration;
                }
            }
        }

        private void serviceAlarms_Completion(object sender, EventArgs e)
        {
            lock (this.threadLock)
            {
                var mp = (IAlarmPlayer)sender;
                this.playStoppedAt[mp.Sound] = this.stamper.GetTimestamp();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeStamp">timestamp of the latest stopped player</param>
        /// <returns>false if some player still plays, true otherwise</returns>
        public bool TryGetLatestTurnAheadAlarmAt(out long timeStamp)
        {
            lock (this.threadLock)
            {
                timeStamp = stamper.GetBeforeTimeTimestamp();

                if (this.turnAheads.Any(x => this.players[x].IsPlaying))
                    return false;

                foreach (AlarmSound sound in this.turnAheads)
                {
                    // sanity-crazy check, but I don't trust Android, 
                    // so we fix here case when we started alarm but it didn't complete
                    if (this.playStoppedAt[sound] < this.playStartedAt[sound])
                    {
                        timeStamp = this.stamper.GetTimestamp();
                        this.playStoppedAt[sound] = timeStamp;
                        return true;
                    }

                    timeStamp = Math.Max(timeStamp, this.playStoppedAt[sound]);
                }

                return true;
            }
        }

        public bool TryAlarm(Alarm alarm, out string reason)
        {
            lock (this.threadLock)
            {
                AlarmSound sound = alarm.GetSound();
                if (sound == AlarmSound.GpsLost || sound == AlarmSound.OffTrack)
                    Common.VibrateAlarm(this.vibrator);

                // https://developer.android.com/reference/android/media/MediaPlayer.html

                {
                    Option<AlarmSound> playing = this.sounds.FirstOrNone(a =>
                    {
                        IAlarmPlayer p = this.players[a];
                        return p != null && p.IsPlaying;
                    });

                    if (playing.HasValue)
                    {
                        reason = $"Cannot play {sound}, {playing.Value} is already playing for {TimeSpan.FromSeconds(stamper.GetSecondsSpan(this.playStartedAt[playing.Value]))}";
                        return false;
                    }
                }

                IAlarmPlayer selected_player = this.players[sound];
                if (selected_player == null)
                {
                    reason = $"No player for {sound}";
                    return false;
                }

                this.playStartedAt[sound] = stamper.GetTimestamp();
                selected_player.Start();
                this.AlarmPlayed?.Invoke(this, alarm);
                reason = null;
                return true;
            }
        }

        public void PostMessage(string reason)
        {
            ; // do nothing, we could send it to UI but this makes little sense
        }

    }
}