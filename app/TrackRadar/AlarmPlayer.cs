using System;
using Android.Media;

namespace TrackRadar
{
    internal sealed class AlarmPlayer : IAlarmPlayer
    {
        private readonly MediaPlayer player;

        public Alarm Alarm { get; }
        public bool IsPlaying => player.IsPlaying;
        public event EventHandler Completion
        {
            add
            {
                player.Completion += value;
            }

            remove
            {
                player.Completion -= value;
            }
        }

        public AlarmPlayer(MediaPlayer player, Alarm alarm)
        {
            this.player = player;
            Alarm = alarm;
        }

        public void Dispose()
        {
            player.Dispose();
        }

        public void Stop()
        {
            player.Stop();
        }

        public void Start()
        {
            player.Start();
        }
    }

  
}