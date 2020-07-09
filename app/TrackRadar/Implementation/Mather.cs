using System.Runtime.CompilerServices;

namespace TrackRadar.Implementation
{
    public static class Mather
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Mod(double x, double y)
        {
            x %= y;
            return x < 0 ? x + y : x;
        }
    }
}