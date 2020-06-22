using Geo;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Geo.Tests
{

    [TestClass]
    public class CalculatorTest
    {
        private const double precision = 0.00000001;

        // this data was verified visually in Google Maps, the point lies outside the arc segment
        GeoPoint point = GeoPoint.FromDegrees(53.1167555264581, 18.631550619914);
        GeoPoint segmentA = GeoPoint.FromDegrees(53.1167316, 18.6314964);
        GeoPoint segmentB = GeoPoint.FromDegrees(53.1167038, 18.6314334);

        [TestMethod]
        public void OppositeSignedDistanceTest()
        {
            var point10 = GeoPoint.FromDegrees(52.92611, 18.807392);
            var point11 = GeoPoint.FromDegrees(52.928361, 18.801384);

            Length sig_len = GeoCalculator.GetSignedDistance(point10, point11);

            var cx1 = GeoPoint.FromDegrees(-52.9279987294512, -161.19764899008);
            var cx2 = GeoCalculator.OppositePoint(cx1);

            Length cx1_p10 = GeoCalculator.GetSignedDistance(cx1, point10);
            Length cx2_p10 = GeoCalculator.GetSignedDistance(cx2, point10);
            Length cx1op_p10 = GeoCalculator.OppositePointSignedDistance(cx1_p10);
            Length cx2op_p10 = GeoCalculator.OppositePointSignedDistance(cx2_p10);

            Assert.AreNotEqual(cx2_p10.Sign(), sig_len.Sign());

            Assert.AreNotEqual(cx1_p10.Sign(), cx2_p10.Sign());

            Assert.AreEqual(cx1op_p10.Sign(), cx2_p10.Sign());
            Assert.AreEqual(cx1_p10.Sign(), cx2op_p10.Sign());

        }
        [TestMethod]
        public void DistanceToArcSegmentTest()
        {
            const double distance = 4.49131315275798;

            Length d1 = GeoCalculator.GetDistanceToArcSegment(point, segmentA, segmentB);
            Assert.AreEqual(distance, d1.Meters, precision);

            Length d2 = GeoCalculator.GetDistanceToArcSegment(point, segmentB, segmentA);
            Assert.AreEqual(distance, d2.Meters, precision);
        }

        [TestMethod]
        public void SignedDistanceTest()
        {
            Length seg_a_point_dist1 = getSignedDistance(segmentA, point);
            Length seg_b_point_dist1 = getSignedDistance(segmentB, point);
            Assert.AreEqual(seg_a_point_dist1.Sign(), seg_b_point_dist1.Sign());

            Length seg_a_point_dist2 = getSignedDistance(point, segmentA);
            Length seg_b_point_dist2 = getSignedDistance(point, segmentB);
            Assert.AreEqual(seg_a_point_dist2.Sign(), seg_b_point_dist2.Sign());

            Assert.AreEqual(seg_a_point_dist1.Sign(), -seg_a_point_dist2.Sign());
            Assert.AreEqual(seg_b_point_dist1.Sign(), -seg_b_point_dist2.Sign());

            Length seg_dist1 = getSignedDistance(segmentA, segmentB);
            Length seg_dist2 = getSignedDistance(segmentB, segmentA);
            Assert.AreEqual(seg_dist1.Sign(), -seg_dist2.Sign());
        }

        private Length getSignedDistance(in GeoPoint a, in GeoPoint b)
        {
            return GeoCalculator.GetDistance(a, b, out double bearing_y, out double bearing_x)
                * GeoCalculator.GetBearingSign(bearing_y, bearing_x);
        }

        private static Angle GetWrappedBearing(in GeoPoint a, in GeoPoint b)
        {
            GeoCalculator.GetDistance(a, b, out double bearing_y, out double bearing_x);
            return GeoCalculator.GetBearing(bearing_y, bearing_x);
        }

        [TestMethod]
        public void BearingTest()
        {
            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint c = GeoPoint.FromDegrees(37.84314, -94.70957);

                Assert.AreEqual(359.178179407564, GeoCalculator.GetBearing(a, c).Degrees, precision);
                Assert.AreEqual(179.173423251363, GeoCalculator.GetBearing(c, a).Degrees, precision);

                Assert.AreEqual(359.178179407564, GetWrappedBearing(a, c).Degrees, precision);
                Assert.AreEqual(179.173423251363, GetWrappedBearing(c, a).Degrees, precision);
            }

            {
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.49093, -94.4816);

                Assert.AreEqual(132.421706629278, GeoCalculator.GetBearing(b, c).Degrees, precision);
                Assert.AreEqual(312.557639755262, GeoCalculator.GetBearing(c, b).Degrees, precision);

                Assert.AreEqual(132.421706629278, GetWrappedBearing(b, c).Degrees, precision);
                Assert.AreEqual(312.557639755262, GetWrappedBearing(c, b).Degrees, precision);
            }

            {
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.70202, -94.47611);

                Assert.AreEqual(74.6943794986585, GeoCalculator.GetBearing(b, c).Degrees, precision);
                Assert.AreEqual(254.833993334593, GeoCalculator.GetBearing(c, b).Degrees, precision);

                Assert.AreEqual(74.6943794986585, GetWrappedBearing(b, c).Degrees, precision);
                Assert.AreEqual(254.833993334593, GetWrappedBearing(c, b).Degrees, precision);
            }

            {
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.84639, -94.58322);

                Assert.AreEqual(26.3178164637247, GeoCalculator.GetBearing(b, c).Degrees, precision);
                Assert.AreEqual(206.392083970563, GeoCalculator.GetBearing(c, b).Degrees, precision);

                Assert.AreEqual(26.3178164637247, GetWrappedBearing(b, c).Degrees, precision);
                Assert.AreEqual(206.392083970563, GetWrappedBearing(c, b).Degrees, precision);
            }
        }

    }
}