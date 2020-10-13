using Geo;
using MathUnit;
using System.Collections.Generic;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public readonly struct TurnArm
    {
        // bearing directs INTO the crossroad (turning point)
        public int SectionId { get; }
        // the last distance is irrelevant and numericaly can be wrong, because it can be distance to some other turn point
        public ArmSectionPoints SectionPoints { get; }

        internal TurnArm(int sectionId, IReadOnlyList<(GeoPoint trackPoint, Length distance)> sectionPoints)
        {
            this.SectionId = sectionId;
            SectionPoints = new ArmSectionPoints( sectionPoints);
        }

#if DEBUG
        public override string ToString()
        {
            return $"{SectionId}";
        }
#endif
    }

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

        public override string ToString()
        {
            return $"{BearingA} X {BearingB}";
        }
    }
}
