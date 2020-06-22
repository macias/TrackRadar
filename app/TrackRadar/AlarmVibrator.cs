using System;
using Android.Media;
using Android.OS;

namespace TrackRadar
{
    internal sealed class AlarmVibrator : IAlarmVibrator
    {
        private readonly Vibrator player;

        public AlarmVibrator(Vibrator player)
        {
            this.player = player;
        }

        public void Dispose()
        {
            this.player.Dispose();
        }

        public void Vibrate(TimeSpan duration)
        {
            this.player.Vibrate((long)(duration.TotalMilliseconds));
        }
   }

}