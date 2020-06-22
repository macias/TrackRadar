using System;
using System.Collections.Generic;
using System.Linq;
using Android.Media;
using Android.OS;
using TrackRadar.Implementation;

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