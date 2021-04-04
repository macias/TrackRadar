using System;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class TestAlarmPlayer : IAlarmPlayer
    {
        public AlarmSound Sound { get; }
        public bool IsPlaying => false;
        public event EventHandler Completion;
        public TimeSpan Duration { get; }

        public TestAlarmPlayer(AlarmSound sound,TimeSpan? duration = null)
        {
            this.Sound = sound;
            this.Duration = duration?? TimeSpan.FromMilliseconds(1000);
    }

        public void Dispose()
        {
            ;
        }

        public void Start()
        {
            this.Completion?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            ;
        }
    }
}