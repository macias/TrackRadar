using Gpx;
using System.Collections.Generic;

namespace TrackRadar
{
    internal sealed class GpxData
    {
        public List<IGeoPoint> Crossroads { get; internal set; }
        public List<GpxTrackSegment> Tracks { get; internal set; }

        public GpxData()
        {
        }
    }
}
