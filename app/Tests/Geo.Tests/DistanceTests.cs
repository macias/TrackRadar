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
        private const double kmMathPrecision = 0.0000000001;

        delegate Length GetDistanceDelegate(in GeoPoint start, in GeoPoint end);

        [TestMethod]
        public void AngularDistanceTest()
        {
            const double m_precision = 0.00001;

            var angle_dist = GeoCalculator.GetLongitudeDifference(Angle.Zero, GeoCalculator.EarthCircumference);
            Assert.AreEqual(Angle.FullCircle.Degrees, angle_dist.Degrees, m_precision);
        }


        [TestMethod]
        public void BearingDistanceMixTest()
        {
            const double m_precision = 0.00001;

            Length dist = Length.FromMeters(100);
            for (int lat = -90; lat <= 90; ++lat)
                for (int lon = -180; lon < 180; ++lon)
                {
                    const int angles = 8;

                    for (int a = 0; a < angles; ++a)
                    {
                        Angle bearing = Angle.PI * (2.0 * a / angles);
                        GeoPoint start = GeoPoint.FromDegrees(lat, lon);
                        GeoPoint end = GeoCalculator.GetDestination(start, bearing, dist);
                        Length result = GeoCalculator.GetDistance(start, end);
                        Assert.AreEqual(dist.Meters, result.Meters, m_precision);
                    }
                }
        }

        [TestMethod]
        public void TestPointsDistanceTest()
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
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // short distance
                var a = GeoPoint.FromDegrees(latitude: 53.864271, longitude: 21.308011);
                var b = GeoPoint.FromDegrees(latitude: 53.863368, longitude: 21.308049);
                var dist1 = measure(a, b);
                Assert.AreEqual(100.43, dist1.Meters, 0.1);
                var dist2 = measure(b, a);
                Assert.AreEqual(dist1.Meters, dist2.Meters, kmMathPrecision * 1000);
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
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = b;
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.486660, longitude: -94.371478);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(102.77, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 37.009531, longitude: -94.620070);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(136.23, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.636415, longitude: -93.221739);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(13.86, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.724441, longitude: -91.851137);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(23.18, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
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
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = b;
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.496902, longitude: -93.223531);
                var b = GeoPoint.FromDegrees(latitude: 36.496902, longitude: -90.151775);
                var c = GeoPoint.FromDegrees(latitude: 36.501227, longitude: -93.368405);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(0.69, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision); // this one fails
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 37.009531, longitude: -94.620070);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(57.60, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.636415, longitude: -93.221739);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(13.86, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
            { // long distance
                var a = GeoPoint.FromDegrees(latitude: 36.511639, longitude: -93.221739);
                var b = GeoPoint.FromDegrees(latitude: 36.499151, longitude: -90.153179);
                var c = GeoPoint.FromDegrees(latitude: 36.724441, longitude: -91.851137);
                var dist1 = measure(c, a, b);
                Assert.AreEqual(23.18, dist1.Kilometers, 0.1);
                var dist2 = measure(c, b, a);
                Assert.AreEqual(dist1.Kilometers, dist2.Kilometers, kmMathPrecision);
            }
        }
    }
}
