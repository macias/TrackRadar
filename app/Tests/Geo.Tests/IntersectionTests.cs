using System;
using Geo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Geo.Tests
{
    [TestClass]
    public class IntersectionTests
    {
        // the precision of computation itself, it has nothing to do with geographic context
        private const double mathPrecision = 0.0000000001;

        [TestMethod]
        public void TestLinesNoIntersection()
        {
            var start1 = GeoPoint.FromDegrees(50, longitude: 18);
            var end1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18.01);
            var start2 = start1;
            var end2 = end1;
            GeoCalculator.GetArcSegmentIntersection(start1, end1, start2, end2, out GeoPoint? p1, out GeoPoint? p2);

            Assert.IsNull(p1);
            Assert.IsNull(p2);
        }

        [TestMethod]
        public void RegularNoIntersection()
        {
            var pa1 = GeoPoint.FromDegrees(latitude: 53.0140713, longitude: 18.6350708);
            var pa2 = GeoPoint.FromDegrees(latitude: 53.0106115, longitude: 18.634985);

            var pb1 = GeoPoint.FromDegrees(latitude: 53.0105937, longitude: 18.6345156);
            var pb2 = GeoPoint.FromDegrees(latitude: 53.0106066, longitude: 18.6359667);

            {
                GeoCalculator.GetArcSegmentIntersection(pa1, pa2, pb1, pb2, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.IsNull(cx1);
                Assert.IsNull(cx2);
            }
            {
                GeoCalculator.GetArcSegmentIntersection(pa2, pa1, pb1, pb2, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.IsNull(cx1);
                Assert.IsNull(cx2);
            }
            {
                GeoCalculator.GetArcSegmentIntersection(pa1, pa2, pb2, pb1, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.IsNull(cx1);
                Assert.IsNull(cx2);
            }
            {
                GeoCalculator.GetArcSegmentIntersection(pa2, pa1, pb2, pb1, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.IsNull(cx1);
                Assert.IsNull(cx2);
            }
        }

        [TestMethod]
        public void RegularIntersection()
        {
            var pa1 = GeoPoint.FromDegrees(latitude: 53.0149896, longitude: 18.6394081);
            var pa2 = GeoPoint.FromDegrees(latitude: 53.0127821, longitude: 18.6496005);

            var pb1 = GeoPoint.FromDegrees(latitude: 53.0146927, longitude: 18.6434636);
            var pb2 = GeoPoint.FromDegrees(latitude: 53.0124851, longitude: 18.6440644);

            var cx = GeoPoint.FromDegrees(latitude: 53.0140749685416, longitude: 18.6436317223972);

            {
                GeoCalculator.GetArcSegmentIntersection(pa1, pa2, pb1, pb2, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.AreEqual(cx.Latitude.Degrees, cx1.Value.Latitude.Degrees, mathPrecision);
                Assert.AreEqual(cx.Longitude.Degrees, cx1.Value.Longitude.Degrees, mathPrecision);
                Assert.IsNull(cx2);
            }
            {
                GeoCalculator.GetArcSegmentIntersection(pa2, pa1, pb1, pb2, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.AreEqual(cx.Latitude.Degrees, cx1.Value.Latitude.Degrees, mathPrecision);
                Assert.AreEqual(cx.Longitude.Degrees, cx1.Value.Longitude.Degrees, mathPrecision);
                Assert.IsNull(cx2);
            }
            {
                GeoCalculator.GetArcSegmentIntersection(pa1, pa2, pb2, pb1, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.AreEqual(cx.Latitude.Degrees, cx1.Value.Latitude.Degrees, mathPrecision);
                Assert.AreEqual(cx.Longitude.Degrees, cx1.Value.Longitude.Degrees, mathPrecision);
                Assert.IsNull(cx2);
            }
            {
                GeoCalculator.GetArcSegmentIntersection(pa2, pa1, pb2, pb1, out GeoPoint? cx1, out GeoPoint? cx2);

                Assert.AreEqual(cx.Latitude.Degrees, cx1.Value.Latitude.Degrees, mathPrecision);
                Assert.AreEqual(cx.Longitude.Degrees, cx1.Value.Longitude.Degrees, mathPrecision);
                Assert.IsNull(cx2);
            }
        }

        [TestMethod]
        public void TestPointsNoIntersection()
        {
            var start1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18);
            var end1 = start1;
            var start2 = start1;
            var end2 = end1;
            GeoCalculator.GetArcSegmentIntersection(start1, end1, start2, end2, out GeoPoint? p1, out GeoPoint? p2);

            Assert.IsNull(p1);
            Assert.IsNull(p2);
        }

        [TestMethod]
        public void TestSharedPointAtZero()
        {
            var start1 = GeoPoint.FromDegrees(latitude: 0, longitude: 0);
            var end1 = GeoPoint.FromDegrees(latitude: 0, longitude: 0.01);
            var start2 = start1;
            var end2 = GeoPoint.FromDegrees(latitude: 0.01, longitude: 0);
            GeoCalculator.GetArcSegmentIntersection(start1, end1, start2, end2, out GeoPoint? p1, out GeoPoint? p2);

            Assert.AreEqual(start1.Latitude.Degrees, p1.Value.Latitude.Degrees, mathPrecision);
            Assert.AreEqual(start1.Longitude.Degrees, p1.Value.Longitude.Degrees, mathPrecision);
            Assert.IsNull(p2);
        }

        // intersection of L-shape with one shared point, there are 4 variants, depending how do you define
        // start and end of each segment
        [TestMethod]
        public void TestSharedPoint1()
        {
            var start1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18);
            var end1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18.01);
            var start2 = start1;
            var end2 = GeoPoint.FromDegrees(latitude: 50.01, longitude: 18);
            GeoCalculator.GetArcSegmentIntersection(start1, end1, start2, end2, out GeoPoint? p1, out GeoPoint? p2);

            Assert.AreEqual(start1.Latitude.Degrees, p1.Value.Latitude.Degrees, mathPrecision);
            Assert.AreEqual(start1.Longitude.Degrees, p1.Value.Longitude.Degrees, mathPrecision);
            Assert.IsNull(p2);
        }

        [TestMethod]
        public void TestSharedPoint2()
        {
            var start1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18);
            var end1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18.01);
            var start2 = GeoPoint.FromDegrees(latitude: 50.01, longitude: 18);
            var end2 = start1;
            GeoCalculator.GetArcSegmentIntersection(start1, end1, start2, end2, out GeoPoint? p1, out GeoPoint? p2);

            Assert.AreEqual(start1.Latitude.Degrees, p1.Value.Latitude.Degrees, mathPrecision);
            Assert.AreEqual(start1.Longitude.Degrees, p1.Value.Longitude.Degrees, mathPrecision);
            Assert.IsNull(p2);
        }

        [TestMethod]
        public void TestSharedPoint3()
        {
            var start1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18.01);
            var end1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18);
            var start2 = end1;
            var end2 = GeoPoint.FromDegrees(latitude: 50.01, longitude: 18);
            GeoCalculator.GetArcSegmentIntersection(start1, end1, start2, end2, out GeoPoint? p1, out GeoPoint? p2);

            Assert.AreEqual(end1.Latitude.Degrees, p1.Value.Latitude.Degrees, mathPrecision);
            Assert.AreEqual(end1.Longitude.Degrees, p1.Value.Longitude.Degrees, mathPrecision);
            Assert.IsNull(p2);
        }

        [TestMethod]
        public void TestSharedPoint4()
        {
            var start1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18.01);
            var end1 = GeoPoint.FromDegrees(latitude: 50, longitude: 18);
            var start2 = GeoPoint.FromDegrees(latitude: 50.01, longitude: 18);
            var end2 = end1;
            GeoCalculator.GetArcSegmentIntersection(start1, end1, start2, end2, out GeoPoint? p1, out GeoPoint? p2);

            Assert.AreEqual(end1.Latitude.Degrees, p1.Value.Latitude.Degrees, mathPrecision);
            Assert.AreEqual(end1.Longitude.Degrees, p1.Value.Longitude.Degrees, mathPrecision);
            Assert.IsNull(p2);
        }

    }
}