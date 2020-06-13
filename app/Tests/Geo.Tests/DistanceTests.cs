using System;
using Geo;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Geo.Tests
{
    [TestClass]
    public class DistanceTests
    {
        // the precision of computation itself, it has nothing to do with geographic context
        private const double mathPrecision = 0.0000000001;

        delegate Length GetDistanceDelegate(in GeoPoint start, in GeoPoint end);

        [TestMethod]
        public void TestPointsDistance()
        {
            // below positions and measurements are accurate to mouse click precision
            // first there was a distance set (Google Maps) and then end points were clicked (again)
            // to read the location

            GetDistanceDelegate measure = GeoCalculator.GetDistance;

            { // zero check
                var a = GeoPoint.FromDegrees(3.222895, 171.719751);
                var dist = measure(a, a);
                Assert.AreEqual(0, dist.Meters);
            }

            { // long distance
                var a = GeoPoint.FromDegrees(53.115282, 18.010172);
                var b = GeoPoint.FromDegrees(52.494307, 16.768717);
                var dist1 = measure(a, b);
                Assert.AreEqual(108.25, dist1.Kilometers, 0.1);
                var dist2 = measure(b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // short distance
                var a = GeoPoint.FromDegrees(latitude: 53.864271, longitude: 21.308011);
                var b = GeoPoint.FromDegrees(latitude: 53.863368, longitude: 21.308049);
                var dist1 = measure(a, b);
                Assert.AreEqual(100.43, dist1.Meters, 0.1);
                var dist2 = measure(b, a);
                Assert.AreEqual(dist1.Meters, dist2.Meters, mathPrecision);
            }
        }

        delegate Length GetDistanceToArcSegmentDelegate(in GeoPoint point, in GeoPoint segmentStart, in GeoPoint segmentEnd);

        [TestMethod]
        public void TestPointToArcSegmentDistance()
        {
            // this is completely un-scientific -- Google Maps + mouse

            GetDistanceToArcSegmentDelegate measure = GeoCalculator.GetDistanceToArcSegment;

            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = a;
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = b;
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.486660, longitude: -94.371478);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(102.77, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 37.009531, longitude: -94.620070);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(136.23, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.636415, longitude: -93.221739);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(13.86, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.724441, longitude: -91.851137);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(23.18, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
        }

        delegate Length GetDistanceToArcDelegate(in GeoPoint point, in GeoPoint arcA, in GeoPoint arcB);

        [TestMethod]
        public void TestPointToArcDistance()
        {
            // this is completely un-scientific -- Google Maps + mouse

            GetDistanceToArcDelegate measure = GeoCalculator.GetDistanceToArc;

            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = a;
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = b;
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.496902, longitude: -93.223531);
                var b = GeoPoint.FromDegrees(latitude: 36.496902, longitude: -90.151775);
                var c = GeoPoint.FromDegrees(latitude: 36.501227, longitude: -93.368405);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0.69, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision); // this one fails
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 37.009531, longitude: -94.620070);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(57.60, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.636415, longitude: -93.221739);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(13.86, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.724441, longitude: -91.851137);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(23.18, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, mathPrecision);
            }
        }
    }
}
