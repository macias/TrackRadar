using Gpx;
using System;
using System.Xml;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class ProximityTrackPointReader : IGpxTrackPointReader<ProximityTrackPoint>
    {
        public bool TryReadBody(XmlReader xmlReader, ProximityTrackPoint point)
        {
            if (xmlReader.Name == GpxSymbol.Proximity)
            {
                point.Proximity = xmlReader.ReadElementContentAsDouble();
                return true;
            }

            return false;
        }

        public bool TryReadExtension(XmlReader xmlReader, ProximityTrackPoint point)
        {
            return false;
        }
    }

}