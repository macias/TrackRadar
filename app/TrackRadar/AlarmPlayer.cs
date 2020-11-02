using System;
using Android.Media;

namespace TrackRadar
{
    internal sealed class AlarmPlayer : IAlarmPlayer
    {
        private readonly MediaPlayer player;

        public AlarmSound Sound { get; }
        public bool IsPlaying => player.IsPlaying;

        public TimeSpan Duration { get; }

        public event EventHandler Completion;//{ add;remove; }
        /*{
            add
            {
                player.Completion += value;
            }

            remove
            {
                player.Completion -= value;
            }
        }*/

        public AlarmPlayer(MediaPlayer player, TimeSpan duration, AlarmSound alarm)
        {
            this.player = player;
            Duration = duration;
            this.player.Completion += playerCompletion;
            Sound = alarm;
        }

        public void Dispose()
        {
            player.Completion -= playerCompletion;
            player.Dispose();
        }

        private void playerCompletion(object sender, EventArgs e)
        {
            this.Completion?.Invoke(this, e);
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