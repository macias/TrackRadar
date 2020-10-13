using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    // just a type alias for easier reading
    public readonly struct ArmSectionPoints
    {
        // the last distance is irrelevant and numericaly can be wrong, because it can be distance to some other turn point
        public IReadOnlyList<(GeoPoint point, Length distance)> TrackPoints { get; }

        public ArmSectionPoints(IReadOnlyList<(GeoPoint point, Length distance)> trackPoints)
        {
            this.TrackPoints = trackPoints;
        }

#if DEBUG
        // this is really fucked up, VS2017 does not show properties for this struct while debugging
        public override string ToString()
        {
            return String.Join(" ; ",TrackPoints.Select(it => $"{it.point} : {it.distance}"));
        }
#endif
    }   
}