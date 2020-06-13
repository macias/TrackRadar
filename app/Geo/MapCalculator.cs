using System.Collections.Generic;
using System.Linq;
using MathUnit;

namespace Geo
{
    public static class MapCalculator
    {
        public static bool TryGetBoundaries(IEnumerable<GeoPoint> points,
            out Angle westmost, out Angle eastmost, out Angle northmost, out Angle southmost)
        {
            westmost = Angle.FromDegrees(361);
            eastmost = Angle.FromDegrees(-1);
            northmost = Angle.FromDegrees(-91);
            southmost = Angle.FromDegrees(+91);

            if (!points.Any())
                return false;

            foreach (GeoPoint p in points)
            {
                westmost = westmost.Min(p.Longitude);
                eastmost = eastmost.Max(p.Longitude);
                southmost = southmost.Min(p.Latitude);
                northmost = northmost.Max(p.Latitude);
            }

            return true;
        }
    }
}