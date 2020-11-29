using System;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    public static class Linqer
    {
        public static IEnumerable<(T value, int index)> ZipIndex<T>(this IEnumerable<T> coll)
        {
            int index = 0;
            foreach (T elem in coll)
            {
                yield return (elem, index);
                ++index;
            }
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T elem in collection)
            {
                action(elem);
                yield return elem;
            }
        }

        public static HashSet<T> AddRange<T>(this HashSet<T> collection, IEnumerable<T> elements)
        {
            foreach (T elem in elements)
                collection.Add(elem);

            return collection;
        }

        public static C RemoveRange<C,T>(this C collection, IEnumerable<T> elements)
            where C : ICollection<T>
        {
            foreach (T elem in elements)
                collection.Remove(elem);

            return collection;
        }
    }
}