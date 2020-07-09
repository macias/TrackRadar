using System;
using System.Threading;

namespace TrackRadar.Implementation
{
    internal sealed class DisposableGuard
    {
        private readonly object threadLock = new object();
        private readonly CountdownEvent countdown;
        private readonly IDisposable emptyDisposable;
        private readonly IDisposable signalDisposable;
        private bool isDiposed;

        public DisposableGuard()
        {
            this.countdown = new CountdownEvent(initialCount: 1);
            this.emptyDisposable = Disposable.Empty;
            this.signalDisposable = Disposable.Create(this.exitEntered);
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