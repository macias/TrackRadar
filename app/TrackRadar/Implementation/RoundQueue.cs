using System;
using System.Collections;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    internal sealed class RoundQueue<T> : IEnumerable<T>
    {
        private readonly List<T> buffer;

        public int Capacity { get; set; }
        public int Count => this.buffer.Count;

        public T this[int index] => this.buffer[index];

        public RoundQueue(int capacity)
        {
            this.Capacity = capacity;
            this.buffer = new List<T>(capacity: capacity);
        }

        public void Enqueue(T value)
        {
            if (this.buffer.Count == Capacity)
                this.buffer.RemoveAt(0);
            this.buffer.Add(value);
        }

        public T Peek()
        {
            return this.buffer[0];
        }

        public IEnumerable<T> Reverse()
        {
            for (int i = this.buffer.Count - 1; i >= 0; --i)
                yield return this.buffer[i];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return buffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return buffer.GetEnumerator();
        }
    }
}