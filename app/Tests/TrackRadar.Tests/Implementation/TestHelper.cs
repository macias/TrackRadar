using System;

namespace TrackRadar.Tests.Implementation
{
    public static class TestHelper
    {
        public static void IsGreaterThan<T>(T actual, T expected, string message = null)
            where T : IComparable<T>
        {
            if (actual.CompareTo(expected) <= 0)
                throw new ArgumentException(message ?? $"Actual {actual} is not greater than expected limit {expected}");
        }
    }
}