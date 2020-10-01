using Geo;
using MathUnit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TrackRadar.Implementation
{
    internal static class MapHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Length AddFlatDistances(Length a, Length b)
        {
            // making this calculation as function to easier find this logical "shortcut"

            // we can add plainly distances, because we don't make shorcuts in the planned tracks
            // yet, there is small error due to the fact, we are on the sphere not the flat surface
            // we hope it is negligble in daily use though
            return a + b;
        }

    }
}