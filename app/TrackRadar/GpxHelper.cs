using Geo;
using Gpx;

namespace TrackRadar
{
    public static class GpxHelper
    {
        internal static GeoPoint FromGpx(IGpxPoint point)
        {
            return new GeoPoint(latitude: point.Latitude, longitude: point.Longitude);
        }        
    }
}
