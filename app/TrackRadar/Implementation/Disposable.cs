using System;

namespace TrackRadar.Implementation
{
    internal sealed class Disposable : IDisposable
    {
        public static IDisposable Empty { get; } = new Disposable();

        public static IDisposable Create(params Action[] actions)
        {
            if (actions.Length == 0)
                return Empty;
            else
                return new Disposable(actions);
        }

        private readonly Action[] actions;

        private Disposable(params Action[] actions)
        {
            this.actions = actions;
        }

        public void Dispose()
        {
            foreach (Action a in this.actions)
                a();
        }
    }
}