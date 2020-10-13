using System;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    public static class Linqer
    {
        public static IEnumerable<(T value,int index)> ZipIndex<T>(this IEnumerable<T> coll)
        {
            int index = 0;
            foreach (T elem in coll)
            {
                yield return (elem, index);
                ++index;
            }
        }

        public static IEnumerable<TElem> ForEach<TElem>(this IEnumerable<TElem> collection,Action<TElem> action)
        {
            foreach (TElem elem in collection)
            {
                action(elem);
                yield return elem;
            }
        }
    }
}