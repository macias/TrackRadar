using System;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
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