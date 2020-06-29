using System;

namespace TrackRadar
{

    // todo: split IDisposable
    public interface IAlarmPlayer : IDisposable
    {
        Alarm Alarm { get; }
        bool IsPlaying { get; }
        event EventHandler Completion;

        void Stop();
        void Start();
    }

   
}