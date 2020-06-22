using MathUnit;

namespace TrackRadar
{
    public readonly struct Turn
    {
        // BOTH bearings direct INTO the crossroad (turning point)
        public Angle BearingA { get; }
        public Angle BearingB { get; }

        public Turn(Angle bearingA, Angle bearingB)
        {
            this.BearingA = bearingA;
            this.BearingB = bearingB;
        }
    }
}
