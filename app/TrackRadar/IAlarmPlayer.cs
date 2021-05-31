using System;

namespace TrackRadar
{
    // todo: split IDisposable
    public interface IAlarmPlayer : IDisposable
    {
        AlarmSound Sound { get; }
        bool IsPlaying { get; }
        TimeSpan Duration { get; }

        event EventHandler Completion;

        void Stop();
        void Start();
    }


}