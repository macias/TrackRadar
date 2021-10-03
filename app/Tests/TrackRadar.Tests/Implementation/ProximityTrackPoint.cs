using Gpx;
using System;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ProximityTrackPoint : GpxTrackPoint
    {
        public double? Proximity { get; set; }
    }

}