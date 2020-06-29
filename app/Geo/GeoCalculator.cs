﻿using MathUnit;
using System;
using System.Runtime.CompilerServices;

namespace Geo
{
    public static partial class GeoCalculator
    {
        public static Length EarthRadius { get; } = Length.FromKilometers(6371);
        public static Length EarthCircumference { get; } = EarthRadius * 2 * Math.PI;
        private const double Radian = Math.PI / 180;

        private static double radiansToDegrees(double radians)
        {
            return radians / Radian;
        }

        private static double degreesToRadians(double degrees)
        {
            return degrees * Radian;
        }

        // intersection has to lie within both segments
        private static bool withinSegment(in GeoPoint cx, in GeoPoint startA, in GeoPoint endA, in GeoPoint startB, in GeoPoint endB,
             Angle angleA, Angle angleB)
        {
            Angle aa1 = angleBetween(startA, cx);
            if (aa1 > angleA)
                return false;
            Angle aa2 = angleBetween(cx, endA);
            if (aa2 > angleA)
                return false;

            Angle ab1 = angleBetween(startB, cx);
            if (ab1 > angleB)
                return false;
            Angle ab2 = angleBetween(cx, endB);
            if (ab2 > angleB)
                return false;

            return true;
        }

        /// <summary>
        /// gives nulls if there is no solution
        /// </summary>
        /// <param name="cx2">if not null, p1 is also not null</param>
        public static void GetArcSegmentIntersection(in GeoPoint startA, in GeoPoint endA, in GeoPoint startB, in GeoPoint endB,
            out GeoPoint? cx1, out GeoPoint? cx2)
        {
            // https://www.movable-type.co.uk/scripts/latlong.html
            // http://blog.mbedded.ninja/mathematics/geometry/spherical-geometry/finding-the-intersection-of-two-arcs-that-lie-on-a-sphere

            if (!TryGetArcIntersection(startA, endA, startB, endB, out GeoPoint cx1_arc, out GeoPoint cx2_arc))
            {
                cx1 = null;
                cx2 = null;
                return;
            }


            Angle angle_a = angleBetween(startA, endA);
            Angle angle_b = angleBetween(startB, endB);

            cx1 = withinSegment(cx1_arc, startA, endA, startB, endB, angle_a, angle_b) ? cx1_arc : (GeoPoint?)null;
            cx2 = withinSegment(cx2_arc, startA, endA, startB, endB, angle_a, angle_b) ? cx2_arc : (GeoPoint?)null;

            if (cx1==null && cx2!=null)
            {
                cx1 = cx2;
                cx2 = null;
            }

        }
        /// <summary>
        /// Finds the bearing from one lat/lon point to another.
        /// </summary>
        /// <returns>bearing in range 0, 360 degrees</returns>
        public static Angle GetBearing(in GeoPoint start, in GeoPoint end)
        {
            // https://en.wikipedia.org/wiki/Bearing_(navigation)#Bearing_measurement

            // http://stackoverflow.com/questions/32771458/distance-from-lat-lng-point-to-minor-arc-segment
            double latA = start.Latitude.Radians;
            double lonA = start.Longitude.Radians;
            double latB = end.Latitude.Radians;
            double lonB = end.Longitude.Radians;

            double delta_lon = lonB - lonA;

            double cos_lat_b = Math.Cos(latB);
            double y = Math.Sin(delta_lon) * cos_lat_b;
            double x = Math.Cos(latA) * Math.Sin(latB) - Math.Sin(latA) * cos_lat_b * Math.Cos(delta_lon);

            return GetBearing(y, x);
        }

        private static Angle angleBetween(in GeoPoint a, in GeoPoint b)
        {
            // todo: explain what kind of angle between points is possible AT ALL???
            // did I mean bearing? here: https://stackoverflow.com/questions/3932502/calculate-angle-between-two-latitude-longitude-points

            // derived from cartesian dot product with unit radius, 
            // and using trig identities to reduce the number of trig functions used
            double lon_cos = (a.Longitude - b.Longitude).Cos();
            double lat_sub_cos = (a.Latitude - b.Latitude).Cos();
            double lat_add_cos = (a.Latitude + b.Latitude).Cos();

            double d = (lon_cos * (lat_sub_cos + lat_add_cos) + (lat_sub_cos - lat_add_cos)) / 2.0;
            double acos = Math.Acos(Math.Min(+1, Math.Max(-1, d)));

            return Angle.FromRadians(acos);
        }

        public static bool TryGetArcIntersection(in GeoPoint startA, in GeoPoint endA, in GeoPoint startB, in GeoPoint endB,
            out GeoPoint p1, out GeoPoint p2)
        {
            // http://blog.mbedded.ninja/mathematics/geometry/spherical-geometry/finding-the-intersection-of-two-arcs-that-lie-on-a-sphere

            if (!TryGetArcIntersection(startA, endA, startB, endB, out p1))
            {
                p2 = default;
                return false;
            }

            p2 = OppositePoint(p1);
            return true;
        }

        public static bool TryGetArcIntersection(in GeoPoint startA, in GeoPoint endA, in GeoPoint startB, in GeoPoint endB, 
            out GeoPoint p1)
        {
            // http://blog.mbedded.ninja/mathematics/geometry/spherical-geometry/finding-the-intersection-of-two-arcs-that-lie-on-a-sphere

            return TryGetArcIntersection(CrossProduct(startA, endA), startB, endB, out p1);
        }

        public static bool TryGetArcIntersection(Vector aCrossProduct, in GeoPoint startB, in GeoPoint endB, out GeoPoint p1)
        {
            Vector b_cp = CrossProduct(startB, endB);

            Vector d = aCrossProduct.CrossProduct(b_cp);

            if (d == Vector.Zero)
            {
                p1 = default;
                return false;
            }

            p1 = d.ToGeoPoint();

            return true;
        }

        public static Length OppositePointSignedDistance(Length len)
        {
            Length result;
            if (len > Length.Zero)
                result = len - GeoCalculator.EarthCircumference / 2;
            else
                result = len + GeoCalculator.EarthCircumference / 2;
            return result;
        }

        public static GeoPoint OppositePoint(in GeoPoint p)
        {
            return new GeoPoint(latitude: -p.Latitude, longitude: p.Longitude + Angle.PI);
        }

        public static Vector CrossProduct(in GeoPoint a, in GeoPoint b)
        {
            // http://www.edwilliams.org/intersect.htm

            double a_lat_cos = a.Latitude.Cos();
            double a_lat_sin = a.Latitude.Sin();

            double b_lat_cos = b.Latitude.Cos();
            double b_lat_sin = b.Latitude.Sin();

            double x = a_lat_cos * a.Longitude.Cos() * b_lat_sin
                - a_lat_sin * b_lat_cos * b.Longitude.Cos();
            double y = a_lat_cos * a.Longitude.Sin() * b_lat_sin
                - a_lat_sin * b_lat_cos * b.Longitude.Sin();
            double z = a_lat_cos * b_lat_cos * (a.Longitude - b.Longitude).Sin();

            return new Vector(x, y, z);
        }

        public static GeoPoint GetMidPoint(in GeoPoint pointA, in GeoPoint pointB)
        {
            // https://www.movable-type.co.uk/scripts/latlong.html

            var λ1 = pointA.Longitude.Radians;
            var φ1 = pointA.Latitude.Radians;
            var λ2 = pointB.Longitude.Radians;
            var φ2 = pointB.Latitude.Radians;

            double cos_φ2 = Math.Cos(φ2);
            double cos_φ1 = Math.Cos(φ1);

            var Bx = cos_φ2 * Math.Cos(λ2 - λ1);
            var By = cos_φ2 * Math.Sin(λ2 - λ1);
            var φ3 = Math.Atan2(Math.Sin(φ1) + Math.Sin(φ2),
                                Math.Sqrt((cos_φ1 + Bx) * (cos_φ1 + Bx) + By * By));
            var λ3 = λ1 + Math.Atan2(By, cos_φ1 + Bx);

            return new GeoPoint(latitude: Angle.FromRadians(φ3), longitude: Angle.FromRadians(λ3));
        }

        public static Length GetDistance(in GeoPoint start, in GeoPoint end)
        {
            return GetDistance(start, end, out _, out _);
        }

        public static Length GetDistance(in GeoPoint start, in GeoPoint end, out double bearingY, out double bearingX)
        {
            // https://en.wikipedia.org/wiki/Great-circle_distance#Computational_formulas

            //double latA = start.Latitude.Radians;
            double latB = end.Latitude.Radians;
            // double lonA = start.Longitude.Radians;
            // double lonB = end.Longitude.Radians;

            //double delta_lon = lonB - lonA;

            double cos_latB = Math.Cos(latB);
            double sin_latB = Math.Sin(latB);
            // double sin_latA = Math.Sin(latA);
            //double cos_latA = Math.Cos(latA);
            //double cos_delta_lon = Math.Cos(delta_lon);

            return GetDistance(start, sin_latB, cos_latB, end.Longitude, out bearingY, out bearingX);
        }

        public static Length GetDistance(in GeoPoint start, double sinEndLatitude, double cosEndLatitude, Angle endLongitude,
            out double bearingY, out double bearingX)
        {
            // https://en.wikipedia.org/wiki/Great-circle_distance#Computational_formulas

            double latA = start.Latitude.Radians;
            //            double latB = end.Latitude.Radians;
            double lonA = start.Longitude.Radians;
            double lonB = endLongitude.Radians;

            double delta_lon = lonB - lonA;

            //  double cos_latB = Math.Cos(latB);
            //double sin_latB = Math.Sin(latB);
            double sin_latA = Math.Sin(latA);
            double cos_latA = Math.Cos(latA);
            double cos_delta_lon = Math.Cos(delta_lon);

            double cosEndLatitude_cos_delta_lon = cosEndLatitude * cos_delta_lon;

            // this is the same expression as we have in calculated bearing
            bearingX = cos_latA * sinEndLatitude - sin_latA * cosEndLatitude_cos_delta_lon;
            bearingY = cosEndLatitude * Math.Sin(delta_lon);

            double y = Math.Sqrt(Math.Pow(bearingY, 2) + Math.Pow(bearingX, 2));
            double x = sin_latA * sinEndLatitude + cos_latA * cosEndLatitude_cos_delta_lon;

            double delta_angle = Math.Atan2(y, x);

            return EarthRadius * Math.Abs(delta_angle);
        }

        public static Length GetSignedDistance(in GeoPoint start, in GeoPoint end)
        {
            Length dist = GetDistance(start, end, out double bearing_y, out double bearing_x);

            return dist * GetBearingSign(bearing_y, bearing_x);
        }

        public static int GetBearingSign(double bearingY, double bearingX)
        {
            int sign_y = Math.Sign(bearingY);
            if (sign_y == 0)
                return Math.Sign(bearingX);
            else
                return sign_y;
        }

        public static GeoPoint GetDestinationPoint(in GeoPoint point, Angle bearing, Length distance)
        {
            var φ1 = point.Latitude;
            var λ1 = point.Longitude;
            // https://www.movable-type.co.uk/scripts/latlong.html
            double d_cos = Math.Cos(distance / EarthRadius);
            double d_sin = Math.Sin(distance / EarthRadius);
            double φ1_sin = φ1.Sin();
            double φ1_cos = φ1.Cos();
            var φ2 = Angle.FromRadians(Math.Asin(φ1_sin * d_cos + φ1_cos * d_sin * bearing.Cos()));
            var λ2 = λ1 + Angle.FromRadians(Math.Atan2(bearing.Sin() * d_sin * φ1_cos, d_cos - φ1_sin * φ2.Sin()));

            return new GeoPoint(latitude: φ2, longitude: λ2);
        }

        public static Length GetDistanceToArcSegment(this in GeoPoint point, in GeoPoint segmentStart, in GeoPoint segmentEnd,
            out GeoPoint crossPoint)
        {
            return getDistanceToArcSegment(point, segmentStart, segmentEnd, out crossPoint, computeCrossPoint: true);
        }

        private static Length getDistanceToArcSegment(in GeoPoint pointP3, in GeoPoint arcP1, in GeoPoint arcP2,
            out GeoPoint crossPoint, bool computeCrossPoint)
        {
            //Length o = getDistanceToArcSegmentOLD(pointP3, arcP1, arcP2, out crossPoint, computeCrossPoint);
            Length n = getDistanceToArcSegmentNEW(pointP3, arcP1, arcP2, out crossPoint, computeCrossPoint);
            return n;
        }

        private static Length getDistanceToArcSegmentOLD<P1, P2, P3>(in GeoPoint pointP3, in GeoPoint arcP1, in GeoPoint arcP2,
            out GeoPoint crossPoint, bool computeCrossPoint)
        {
            // http://stackoverflow.com/questions/32771458/distance-from-lat-lng-point-to-minor-arc-segment
            // https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment/865080#865080
            // CROSSARC Calculates the shortest distance in meters 
            // between an arc (defined by p1 and p2) and a third point, p3.

            double R = EarthRadius.Meters;

            // Prerequisites for the formulas
            double bear12 = GetBearing(arcP1, arcP2).Radians;
            double bear13 = GetBearing(arcP1, pointP3).Radians;
            Length dis13 = GeoCalculator.GetDistance(arcP1, pointP3);

            // Is relative bearing obtuse?
            double bearing_diff = Math.Abs(bear13 - bear12);
            if (bearing_diff > Math.PI)
                bearing_diff = 2 * Math.PI - bearing_diff;
            if (bearing_diff > (Math.PI / 2))
            {
                crossPoint = arcP1;
                return dis13;
            }
            else
            {
                // Find the cross-track distance.
                double dxt = Math.Asin(Math.Sin(dis13.Meters / R) * Math.Sin(bear13 - bear12)) * R;
                // Is p4 beyond the arc?
                Length dis12 = GeoCalculator.GetDistance(arcP1, arcP2);
                double dis14 = Math.Acos(Math.Cos(dis13.Meters / R) / Math.Cos(dxt / R)) * R;
                if (dis14 > dis12.Meters)
                {
                    crossPoint = arcP2;
                    return GeoCalculator.GetDistance(arcP2, pointP3);
                }
                else
                {
                    crossPoint = computeCrossPoint
                        ? GetDestinationPoint(arcP1, Angle.FromRadians(bear12), Length.FromMeters(dis14))
                        : default;

                    return Length.FromMeters(Math.Abs(dxt));
                }
            }
        }

        private static Length getDistanceToArcSegmentNEW(in GeoPoint pointP3, in GeoPoint arcP1, in GeoPoint arcP2,
            out GeoPoint crossPoint, bool computeCrossPoint)
        {
            // http://stackoverflow.com/questions/32771458/distance-from-lat-lng-point-to-minor-arc-segment
            // https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment/865080#865080
            // CROSSARC Calculates the shortest distance in meters 
            // between an arc (defined by p1 and p2) and a third point, p3.

            double R = EarthRadius.Meters;

            // Prerequisites for the formulas
            Length dis12;
            double bear12;
            {
                dis12 = GeoCalculator.GetDistance(arcP1, arcP2, out double bear12y, out double bear12x);
                bear12 = Math.Atan2(bear12y, bear12x);
            }
            Length dis13;
            double bear13;
            {
                dis13 = GeoCalculator.GetDistance(arcP1, pointP3, out double bear13y, out double bear13x);
                bear13 = Math.Atan2(bear13y, bear13x);
            }

            // Is relative bearing obtuse?
            double bearing_diff = Math.Abs(bear13 - bear12);
            if (bearing_diff > Math.PI)
                bearing_diff = 2 * Math.PI - bearing_diff;
            if (bearing_diff > (Math.PI / 2))
            {
                crossPoint = arcP1;
                return dis13;
            }
            else
            {
                // Find the cross-track distance.
                double dxt = Math.Asin(Math.Sin(dis13.Meters / R) * Math.Sin(bear13 - bear12)) * R;
                // Is p4 beyond the arc?
                double dis14 = Math.Acos(Math.Cos(dis13.Meters / R) / Math.Cos(dxt / R)) * R;
                if (dis14 > dis12.Meters)
                {
                    crossPoint = arcP2;
                    return GeoCalculator.GetDistance(arcP2, pointP3);
                }
                else
                {
                    crossPoint = computeCrossPoint
                        ? GetDestinationPoint(arcP1, Angle.FromRadians(bear12), Length.FromMeters(dis14))
                        : default;

                    return Length.FromMeters(Math.Abs(dxt));
                }
            }
        }

        public static Angle GetBearing(double bearingY, double bearingX)
        {
            double bearing = Math.Atan2(bearingY, bearingX);
            if (bearing < 0)
                bearing += Math.PI * 2;
            return Angle.FromRadians(bearing);
        }

        public static Length GetDistanceToArcSegment(this in GeoPoint point, in GeoPoint segmentStart, in GeoPoint segmentEnd)
        {
            return getDistanceToArcSegment(point, segmentStart, segmentEnd, out _, computeCrossPoint: false);
        }

        public static Length GetDistanceToArc(this in GeoPoint point, in GeoPoint arcA, in GeoPoint arcB)
        {
            // http://stackoverflow.com/questions/32771458/distance-from-lat-lng-point-to-minor-arc-segment
            // simplification of the above

            // see also:
            // http://stackoverflow.com/a/20369652/6734314
            // http://www.movable-type.co.uk/scripts/latlong.html

            double R = EarthRadius.Meters;

            // Prerequisites for the formulas
            double bear12 = GetBearing(arcA, arcB).Radians;
            double bear13 = GetBearing(arcA, point).Radians;
            Length dis13 = GeoCalculator.GetDistance(arcA, point);

            // Find the cross-track distance.
            double dxt = Math.Asin(Math.Sin(dis13.Meters / R) * Math.Sin(bear13 - bear12)) * R;
            return Length.FromMeters(Math.Abs(dxt));
        }

        public static GeoPoint GetDestination(in GeoPoint start, Angle bearing, Length distance)
        {
            // https://www.movable-type.co.uk/scripts/latlong.html

            var φ1 = start.Latitude;
            var λ1 = start.Longitude;

            double angle_dist = distance / EarthRadius;
            double dist_cos = Math.Cos(angle_dist);
            double dist_sin = Math.Sin(angle_dist);

            double φ1_sin = φ1.Sin();
            double φ1_cos = φ1.Cos();

            var φ2 = Math.Asin(φ1_sin * dist_cos + φ1_cos * dist_sin * bearing.Cos());
            var λ2 = λ1.Radians + Math.Atan2(bearing.Sin() * dist_sin * φ1_cos,
                                     dist_cos - φ1_sin * Math.Sin(φ2));

            return new GeoPoint(latitude: Angle.FromRadians(φ2), longitude: Angle.FromRadians(λ2));
        }

    }
}