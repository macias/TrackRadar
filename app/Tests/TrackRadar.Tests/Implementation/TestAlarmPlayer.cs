using System;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class TestAlarmPlayer : IAlarmPlayer
    {
        private readonly ITickingTimeStamper tickingStamper;
        private long playedAt;

        public AlarmSound Sound { get; }
        private bool isPlaying;
        public bool IsPlaying { get { CheckCompletion(); return this.isPlaying; } }
        public event EventHandler Completion;
        public TimeSpan Duration { get; }

        public TestAlarmPlayer(AlarmSound sound, ITickingTimeStamper tickingStamper, TimeSpan? duration = null)
        {
            this.Sound = sound;
            this.Duration = duration ?? TimeSpan.FromMilliseconds(1000);
            this.tickingStamper = tickingStamper;
            if (this.tickingStamper != null)
            {
                this.playedAt = tickingStamper.GetBeforeTimeTimestamp();
                this.tickingStamper.TimePassed += TickingStamper_TimePassed;
            }
        }

        public void Dispose()
        {
            if (this.tickingStamper != null)
            {
                this.tickingStamper.TimePassed -= TickingStamper_TimePassed;
            }
        }

        private void TickingStamper_TimePassed(object sender, TimeSpan time)
        {
            CheckCompletion();
        }

        public void Start()
        {
            if (this.tickingStamper == null)
                this.Completion?.Invoke(this, EventArgs.Empty);
            else
            {
                this.isPlaying = true;
                this.playedAt = this.tickingStamper.GetTimestamp();
                CheckCompletion();
            }
        }

        private void CheckCompletion()
        {
            if (!isPlaying)
                return;

            if (this.tickingStamper.GetSecondsSpan(this.playedAt) >= this.Duration.TotalSeconds)
            {
                isPlaying = false;
                this.Completion?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Stop()
        {
            if (this.tickingStamper!=null)
            this.playedAt = tickingStamper.GetBeforeTimeTimestamp();
        }
    }
}