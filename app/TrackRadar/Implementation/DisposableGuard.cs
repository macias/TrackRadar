using System;
using System.Threading;

namespace TrackRadar.Implementation
{
    internal sealed class DisposableGuard
    {
        private sealed class Disposable : IDisposable
        {
            private readonly DisposableGuard guard;

            public Disposable(DisposableGuard guard)
            {
                this.guard = guard;
            }

            public void Dispose()
            {
                this.guard?.exitEntered();
            }
        }

        private readonly object threadLock = new object();
        private readonly CountdownEvent countdown;
        private readonly Disposable emptyDisposable;
        private readonly Disposable signalDisposable;
        private bool isDiposed;

        public DisposableGuard()
        {
            this.countdown = new CountdownEvent(initialCount: 1);
            this.emptyDisposable = new Disposable(null);
            this.signalDisposable = new Disposable(this);
        }

        public void Dispose()
        {
            lock (this.threadLock)
            {
                if (this.isDiposed)
                    return;

                this.isDiposed = true;
                this.countdown.Signal();
            }

            this.countdown.Wait();
            this.countdown.Dispose();
        }

        public IDisposable TryEnter(out bool allowed)
        {
            lock (this.threadLock)
            {
                allowed = !this.isDiposed;
                if (allowed)
                {
                    this.countdown.AddCount(1);
                    return signalDisposable;
                }
                else
                    return emptyDisposable;

            }
        }

        private void exitEntered()
        {
            this.countdown.Signal();
        }
    }
}