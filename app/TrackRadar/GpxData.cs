using Geo;
using Gpx;
using System.Collections.Generic;

namespace TrackRadar
{
    internal sealed class GpxData
    {
        public List<IGeoPoint> Crossroads { get; internal set; }
        public IGeoMap<Segment> Map { get; internal set; }

        public GpxData()
        {
        }
    }
}
