using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TrackRadar.Implementation
{
    public static class EnumHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> GetValues<T>()
        {
            foreach (var elem in Enum.GetValues(typeof(T)))
                yield return (T)elem;
        }
    }
}