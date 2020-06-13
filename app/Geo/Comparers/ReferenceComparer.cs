using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Geo.Comparers
{
    public sealed class ReferenceComparer<T> : IEqualityComparer<T>
    {
        public static IEqualityComparer<T> Default { get; } = new ReferenceComparer<T>();

        private ReferenceComparer()
        {

        }

        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}