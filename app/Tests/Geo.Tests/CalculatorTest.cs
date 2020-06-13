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
        public void CheckOppositeSignedDistance()
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

            Assert.AreNotEqual(cx2_p10.Sign(),sig_len.Sign());

            Assert.AreNotEqual(cx1_p10.Sign(), cx2_p10.Sign());

            Assert.AreEqual(cx1op_p10.Sign(), cx2_p10.Sign());
            Assert.AreEqual(cx1_p10.Sign(), cx2op_p10.Sign());

        }
        [TestMethod]
        public void CheckDistanceToArcSegment()
        {
            const double distance = 4.49131315275798;

            Length d1 = GeoCalculator.GetDistanceToArcSegment(point, segmentA, segmentB);
            Assert.AreEqual(distance, d1.Meters, precision);

            Length d2 = GeoCalculator.GetDistanceToArcSegment(point, segmentB, segmentA);
            Assert.AreEqual(distance, d2.Meters, precision);
        }

        [TestMethod]
        public void CheckSignedDistance()
        {
            Length seg_a_point_dist1 = GetSignedDistance(segmentA, point);
            Length seg_b_point_dist1 = GetSignedDistance(segmentB, point);
            Assert.AreEqual(seg_a_point_dist1.Sign(), seg_b_point_dist1.Sign());

            Length seg_a_point_dist2 = GetSignedDistance(point, segmentA);
            Length seg_b_point_dist2 = GetSignedDistance(point, segmentB);
            Assert.AreEqual(seg_a_point_dist2.Sign(), seg_b_point_dist2.Sign());

            Assert.AreEqual(seg_a_point_dist1.Sign(), -seg_a_point_dist2.Sign());
            Assert.AreEqual(seg_b_point_dist1.Sign(), -seg_b_point_dist2.Sign());

            Length seg_dist1 = GetSignedDistance(segmentA, segmentB);
            Length seg_dist2 = GetSignedDistance(segmentB, segmentA);
            Assert.AreEqual(seg_dist1.Sign(), -seg_dist2.Sign());
        }

        private Length GetSignedDistance(in GeoPoint a, in GeoPoint b)
        {
            return GeoCalculator.GetDistance(a, b, out double bearing_y, out double bearing_x)
                * GeoCalculator.GetBearingSign(bearing_y, bearing_x);
        }
    }
}