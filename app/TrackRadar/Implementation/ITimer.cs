using System;
using System.Threading;

namespace TrackRadar.Implementation
{
    internal interface ITimer : IDisposable
    {
        void Change(TimeSpan dueTime, TimeSpan period);
    }
}