using Geo;
using MathUnit;
using System;
using System.Collections.Generic;

namespace TrackRadar.Implementation
{
    internal readonly struct TurnPointInfo
    {
        public GeoPoint TurnPoint { get; }
        public Length Distance { get; }

        public TurnPointInfo(GeoPoint turnPoint,Length distance)
        {
            TurnPoint = turnPoint;
            Distance = distance;
        }
    }
}