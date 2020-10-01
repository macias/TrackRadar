using System;
using System.Collections.Generic;

namespace TrackRadar.Comparers
{
    public static class SelectComparer
    {
        public static IEqualityComparer<TInput> Create<TInput, TOutput>(Func<TInput, TOutput> projection)
        {
            return new Comparer<TInput, TOutput>(projection);

        }

        private sealed class Comparer<TInput, TOutput> : IEqualityComparer<TInput>
        {
            private readonly Func<TInput, TOutput> projection;

            public Comparer(Func<TInput, TOutput> projection)
            {
                this.projection = projection;
            }

            public bool Equals(TInput x, TInput y) => EqualityComparer<TOutput>.Default.Equals(projection(x), projection(y));
            public int GetHashCode(TInput obj) => EqualityComparer<TOutput>.Default.GetHashCode(projection(obj));
        }
    }
}