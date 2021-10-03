using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    internal static class LinqExtension
    {
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

        public static C RemoveRange<C, T>(this C collection, IEnumerable<T> elements)
            where C : ICollection<T>
        {
            foreach (T elem in elements)
                collection.Remove(elem);

            return collection;
        }

        public static Option<T> FirstOrNone<T>(this IEnumerable<T> coll)
        {
            IEnumerator<T> iter = coll.GetEnumerator();
            if (iter.MoveNext())
                return new Option<T>(iter.Current);
            else
                return Option<T>.None;
        }
        public static Option<T> MaxOrNone<T>(this IEnumerable<T> coll)
        {
            if (coll.Any())
                return new Option<T>(coll.Max());
            else
                return Option<T>.None;
        }

        public static Option<T> FirstOrNone<T>(this IEnumerable<T> coll,Func<T,bool> pred)
        {
            return coll.Where(pred).FirstOrNone();
        }

        public static IEnumerable<T> GetEnums<T>()
            where T : struct
        {
            foreach (object elem in Enum.GetValues(typeof(T)))
                yield return (T)elem;
        }
    }
}