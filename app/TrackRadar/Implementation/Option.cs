using System;

namespace TrackRadar.Implementation
{
    internal readonly struct Option<T>
    {
        public static Option<T> None { get; } = new Option<T>();

        public bool HasValue { get; }

        private readonly T value;
        public T Value => HasValue ? value : throw new InvalidOperationException("None has no value.");

        public Option(T value)
        {
            this.value = value;
            this.HasValue = true;
        }
    }
}