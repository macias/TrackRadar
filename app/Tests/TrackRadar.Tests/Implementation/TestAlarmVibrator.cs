using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class TestAlarmVibrator : IAlarmVibrator
    {
        public void Dispose()
        {
        }

        public void Vibrate(TimeSpan duration)
        {
        }
    }

}