using System;

namespace TrackRadar.Tests.Implementation
{
    public static class TestHelper
    {
        public static void IsGreaterThan<T>(T greater, T less, string message = null)
            where T : IComparable<T>
        {
            if (greater.CompareTo(less) <= 0)
                throw new ArgumentException(message ?? $"{greater} is not greater than {less}");
        }

        public static void IsGreaterEqual<T>(T greater, T less, string message = null)
            where T : IComparable<T>
        {
            if (greater.CompareTo(less) < 0)
                throw new ArgumentException(message ?? $"{greater} is not greater or equal to {less}");
        }
    }
}