using MathUnit;
using System;

namespace TrackRadar
{
    public sealed class DistanceEventArgs : EventArgs
    {
        public double FenceDistance { get; }
        public Length TotalClimbs { get; }
        public Length RidingDistance { get; }
        public TimeSpan RidingTime { get; }
        public Speed TopSpeed { get; }

        public DistanceEventArgs(double fenceDistance,Length totalClimbs, Length ridingDistance,TimeSpan ridingTime,Speed topSpeed)
        {
            this.FenceDistance = fenceDistance;
            TotalClimbs = totalClimbs;
            RidingDistance = ridingDistance;
            RidingTime = ridingTime;
            TopSpeed = topSpeed;
        }
    }
}