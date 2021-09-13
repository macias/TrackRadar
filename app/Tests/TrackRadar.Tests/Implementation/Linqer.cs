using System;
using System.Collections.Generic;

namespace TrackRadar.Tests.Implementation
{
    // duplicate from TrackRadar (because tuples are not compatible)
    internal static class Linqer
    {
        public static IEnumerable<(T value, int index)> ZipIndex<T>(IEnumerable<T> coll)
        {
            int index = 0;
            foreach (T elem in coll)
            {
                yield return (elem, index);
                ++index;
            }
        }
    }
}