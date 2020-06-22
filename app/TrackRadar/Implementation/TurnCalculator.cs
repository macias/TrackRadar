using Geo;
using MathUnit;

namespace TrackRadar.Implementation
{
    internal static class TurnCalculator
    {
        internal static bool TryComputeTurn(in GeoPoint cx, IGeoMap map, Length turnAheadDistance, out Turn turn)
        {
            if (tryFindTurnSegments(cx, map, turnAheadDistance,
                out double aArmBearingY, out double aArmBearingX, out double bArmBearingY, out double bArmBearingX))
            {
                Angle a_bearing = GeoCalculator.GetBearing(aArmBearingY, aArmBearingX);
                Angle b_bearing = GeoCalculator.GetBearing(bArmBearingY, bArmBearingX);
                turn = new Turn(a_bearing, b_bearing);
                return true;
            }

            turn = default;
            return false;
        }

        private static bool tryFindTurnSegments(in GeoPoint crossroad, IGeoMap map,
            Length range, out double aArmBearingY, out double aArmBearingX, out double bArmBearingY, out double bArmBearingX)
        {
            aArmBearingY = aArmBearingX = bArmBearingY = bArmBearingX = default;

            bool a_found = false;
            bool b_found = false;

            double cx_sin_lat = crossroad.Latitude.Sin();
            double cx_cos_lat = crossroad.Latitude.Cos();

            foreach (ISegment seg in map.GetNearby(crossroad,range))
            {
                // both bearing have to be towards crossroad
                Length dist1 = GeoCalculator.GetDistance(seg.A, sinEndLatitude: cx_sin_lat, cosEndLatitude: cx_cos_lat, 
                    endLongitude: crossroad.Longitude, out double bearing1_y, out double bearing1_x);
                Length dist2 = GeoCalculator.GetDistance(seg.B, sinEndLatitude: cx_sin_lat, cosEndLatitude: cx_cos_lat, 
                    endLongitude: crossroad.Longitude, out double bearing2_y, out double bearing2_x);
                bool switched = false;
                if (dist1 > dist2)
                {
                    (dist1, dist2) = (dist2, dist1);
                    switched = true;
                }

                if (dist1 <= range && dist2 >= range)
                {
                    // found it
                    if (!a_found)
                    {
                        // take the more distant
                        aArmBearingY = switched ? bearing1_y : bearing2_y;
                        aArmBearingX = switched ? bearing1_x : bearing2_x;
                        a_found = true;
                    }
                    else if (!b_found)
                    {
                        // take the more distant
                        bArmBearingY = switched ? bearing1_y : bearing2_y;
                        bArmBearingX = switched ? bearing1_x : bearing2_x;
                        b_found = true;
                    }
                    else
                    {
                        return false;// multiple segments, cannot tell
                    }
                }
            }

            return a_found && b_found;
        }

    }
}