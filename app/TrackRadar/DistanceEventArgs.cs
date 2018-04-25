using System;

namespace TrackRadar
{
    public sealed class DistanceEventArgs : EventArgs
    {
        public double Distance { get; }

        public DistanceEventArgs(double dist)
        {
            this.Distance = dist;
        }
    }
}