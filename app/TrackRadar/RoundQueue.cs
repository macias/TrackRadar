using System;
using System.Collections;
using System.Collections.Generic;

namespace TrackRadar
{
    public class RoundQueue<T> : IEnumerable<T>
    {
        private readonly int size;
        private readonly Queue<T> buffer;

        public RoundQueue(int size)
        {
            this.size = size;
            this.buffer = new Queue<T>(capacity: size);
        }

        public void Enqueue(T value)
        {
            if (this.buffer.Count == size)
                this.buffer.Dequeue();
            this.buffer.Enqueue(value);
        }

        public T Peek()
        {
            return this.buffer.Peek();
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