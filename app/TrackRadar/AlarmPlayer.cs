using System;
using Android.Media;

namespace TrackRadar
{
    internal sealed class AlarmPlayer : IAlarmPlayer
    {
        private readonly MediaPlayer player;

        public Alarm Alarm { get; }
        public bool IsPlaying => player.IsPlaying;
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

        public AlarmPlayer(MediaPlayer player, Alarm alarm)
        {
            this.player = player;
            this.player.Completion += playerCompletion;
            Alarm = alarm;
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