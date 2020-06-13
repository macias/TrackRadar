using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    internal static class LinqExtension
    {
        public static Option<T> FirstOrNone<T>(this IEnumerable<T> coll)
        {
            IEnumerator<T> iter = coll.GetEnumerator();
            if (iter.MoveNext())
                return new Option<T>(iter.Current);
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