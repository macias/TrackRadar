using MathUnit;
using System;

namespace Geo
{
    public readonly struct ArcSegmentIntersection
    {
        public Length SegmentLength { get; }
        //public Angle SegmentBearing { get; }
        public GeoPoint Intersection { get; }
        public Length AlongSegmentDistance { get; }

        public ArcSegmentIntersection(Length segmentLength, 
            //Angle segmentBearing,
            GeoPoint intersection, Length alongSegmentDistance)
        {
            SegmentLength = segmentLength;
            //SegmentBearing = segmentBearing;
            Intersection = intersection;
            AlongSegmentDistance = alongSegmentDistance;
        }
    }

}