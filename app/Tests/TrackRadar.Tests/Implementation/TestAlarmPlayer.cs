using System;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class TestAlarmPlayer : IAlarmPlayer
    {
        public Alarm Alarm { get; }
        public bool IsPlaying => false;
        public event EventHandler Completion;

        public TestAlarmPlayer(Alarm alarm)
        {
            this.Alarm = alarm;
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