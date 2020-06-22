namespace TrackRadar.Implementation
{
    public sealed class ThreadSafe<T>
    {
        private readonly object threadLock = new object();

        private T value;
        public T Value
        {
            get { lock (threadLock) return this.value; }
            set { lock (threadLock) this.value = value; }
        }

        public ThreadSafe()
        {

        }

        public ThreadSafe(T value)
        {
            this.value = value;
        }
    }
}