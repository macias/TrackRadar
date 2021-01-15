using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Geo
{
    public sealed class SufficientlySameComparer : IEqualityComparer<GeoPoint>
    {
        public static IEqualityComparer<GeoPoint> Default { get; } = new SufficientlySameComparer();

        private const int decimals = 7; // Google Maps uses 7 places and it lives...
        private const MidpointRounding mode = MidpointRounding.AwayFromZero;

        private SufficientlySameComparer()
        {

        }

        public bool Equals(GeoPoint a, GeoPoint b)
        {
            return Math.Round(a.Latitude.Degrees, decimals, mode) == Math.Round(b.Latitude.Degrees, decimals, mode)
                && Math.Round(a.Longitude.Degrees, decimals, mode) == Math.Round(b.Longitude.Degrees, decimals, mode);
        }

        public int GetHashCode(GeoPoint obj)
        {
            return Math.Round(obj.Latitude.Degrees, decimals, mode).GetHashCode()
                ^ Math.Round(obj.Longitude.Degrees, decimals, mode).GetHashCode();
        }
    }

    public static class Mather
    {
        public static bool SufficientlySame(in GeoPoint a, in GeoPoint b)
        {
            return SufficientlySameComparer.Default.Equals(a, b);
        }

        public static double MakeCpuBusy()
        {
            // PC: 5-6 seconds
            // Galaxy Ace 2: 33 seconds
            long start = Stopwatch.GetTimestamp();
            for (int my_lat = -90; my_lat <= 90; ++my_lat)
                for (int my_lon = -180; my_lon < 180; ++my_lon)
                    for (int a_lat = -90; a_lat <= 90; a_lat += 30)
                        for (int a_lon = -180; a_lon < 180; a_lon += 60)
                            GeoCalculator.GetDistanceToArcSegment(GeoPoint.FromDegrees(my_lat, my_lon),
                        GeoPoint.FromDegrees(a_lat, a_lon), GeoPoint.FromDegrees(+20, 140), out GeoPoint cx);
            return (Stopwatch.GetTimestamp() - start - 0.0) / Stopwatch.Frequency;
        }

    }
}