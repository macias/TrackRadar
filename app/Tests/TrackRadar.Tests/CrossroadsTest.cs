using Geo;
using Gpx;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    [TestClass]
    public class CrossroadsTest
    {
        private const double precision = 0.00000001;

        [TestMethod]
        public void ExtensionTest()
        {
            const string filename = @"Data/extension.gpx";

            IPlanData gpx_data = GpxLoader.ReadGpx(filename, Length.FromMeters(100), onProgress: null, CancellationToken.None);
            Assert.IsTrue(GpxLoader.tryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            Assert.AreEqual(0, gpx_data.Crossroads.Count);
        }

        [TestMethod]
        public void DoubleIntersectionTest()
        {
            const string filename = @"Data/double-intersection.gpx";

            IPlanData gpx_data = GpxLoader.ReadGpx(filename, Length.FromMeters(100), onProgress: null, CancellationToken.None);
            Assert.IsTrue(GpxLoader.tryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            // saveGpx(gpx_data.Crossroads);

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            Assert.AreEqual(2, gpx_data.Crossroads.Count);

            Assert.AreEqual(38.8478391592013, gpx_data.Crossroads[0].Latitude.Degrees, precision);
            Assert.AreEqual(-3.71607968045339, gpx_data.Crossroads[0].Longitude.Degrees, precision);

            Assert.AreEqual(38.8425854026487, gpx_data.Crossroads[1].Latitude.Degrees, precision);
            Assert.AreEqual(-3.71669889530884, gpx_data.Crossroads[1].Longitude.Degrees, precision);
        }

        [TestMethod]
        public void CrossroadsTotalTest()
        {
            const string filename = @"Data/crossroads-total.gpx";

            IPlanData gpx_data = GpxLoader.ReadGpx(filename, Length.FromMeters(100), onProgress: null, CancellationToken.None);
            Assert.IsTrue(GpxLoader.tryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));


            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            Assert.AreEqual(13, gpx_data.Crossroads.Count);

            Assert.AreEqual(16.8936197293979, gpx_data.Crossroads[0].Latitude.Degrees, precision);
            Assert.AreEqual(3.1787316182833, gpx_data.Crossroads[0].Longitude.Degrees, precision);

            Assert.AreEqual(16.8887736999992, gpx_data.Crossroads[1].Latitude.Degrees, precision);
            Assert.AreEqual(3.13779059999673, gpx_data.Crossroads[1].Longitude.Degrees, precision);

            Assert.AreEqual(16.8076035000023, gpx_data.Crossroads[2].Latitude.Degrees, precision);
            Assert.AreEqual(3.10079109998162, gpx_data.Crossroads[2].Longitude.Degrees, precision);

            Assert.AreEqual(16.8592095197273, gpx_data.Crossroads[3].Latitude.Degrees, precision);
            Assert.AreEqual(3.14991028487666, gpx_data.Crossroads[3].Longitude.Degrees, precision);

            Assert.AreEqual(16.8396966193188, gpx_data.Crossroads[4].Latitude.Degrees, precision);
            Assert.AreEqual(3.16075355534243, gpx_data.Crossroads[4].Longitude.Degrees, precision);

            Assert.AreEqual(16.8064280027882, gpx_data.Crossroads[5].Latitude.Degrees, precision);
            Assert.AreEqual(3.12732316838546, gpx_data.Crossroads[5].Longitude.Degrees, precision);

            Assert.AreEqual(16.8024285999989, gpx_data.Crossroads[6].Latitude.Degrees, precision);
            Assert.AreEqual(3.20027810000194, gpx_data.Crossroads[6].Longitude.Degrees, precision);

            Assert.AreEqual(16.8418774601993, gpx_data.Crossroads[7].Latitude.Degrees, precision);
            Assert.AreEqual(3.15659887886566, gpx_data.Crossroads[7].Longitude.Degrees, precision);

            Assert.AreEqual(16.8605446415007, gpx_data.Crossroads[8].Latitude.Degrees, precision);
            Assert.AreEqual(3.13797407027238, gpx_data.Crossroads[8].Longitude.Degrees, precision);

            Assert.AreEqual(16.858500900001, gpx_data.Crossroads[9].Latitude.Degrees, precision);
            Assert.AreEqual(3.16086709999214, gpx_data.Crossroads[9].Longitude.Degrees, precision);

            Assert.AreEqual(16.8752200999987, gpx_data.Crossroads[10].Latitude.Degrees, precision);
            Assert.AreEqual(3.13840510000206, gpx_data.Crossroads[10].Longitude.Degrees, precision);

            Assert.AreEqual(16.843227172543, gpx_data.Crossroads[11].Latitude.Degrees, precision);
            Assert.AreEqual(3.16077687101759, gpx_data.Crossroads[11].Longitude.Degrees, precision);

            Assert.AreEqual(16.8544919000013, gpx_data.Crossroads[12].Latitude.Degrees, precision);
            Assert.AreEqual(3.18811959999266, gpx_data.Crossroads[12].Longitude.Degrees, precision);
        }

        [TestMethod]
        public void IntersectionTest()
        {
            const string filename = @"Data/intersection.gpx";

            IPlanData gpx_data = GpxLoader.ReadGpx(filename, Length.FromMeters(100), onProgress: null, CancellationToken.None);
            Assert.IsTrue(GpxLoader.tryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            Assert.AreEqual(1, gpx_data.Crossroads.Count);

            GeoPoint pt = gpx_data.Crossroads.Single();
            Assert.AreEqual(38.815190752937724, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.79047983062, pt.Longitude.Degrees, precision);
        }

        [TestMethod]
        public void IntersectionApartTest()
        {
            const string filename = @"Data/intersection-apart.gpx";

            IPlanData gpx_data = GpxLoader.ReadGpx(filename, Length.FromMeters(100), onProgress: null, CancellationToken.None);
            Assert.IsTrue(GpxLoader.tryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            Assert.AreEqual(1, gpx_data.Crossroads.Count);

            GeoPoint pt = gpx_data.Crossroads.Single();
            Assert.AreEqual(38.7864379704086, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.8086570696210611, pt.Longitude.Degrees, precision);
        }

        [TestMethod]
        public void PassingByTest()
        {
            const string filename = @"Data/passing-by.gpx";

            IPlanData gpx_data = GpxLoader.ReadGpx(filename, Length.FromMeters(100), onProgress: null, CancellationToken.None);
            Assert.IsTrue(GpxLoader.tryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            Assert.AreEqual(1, gpx_data.Crossroads.Count);

            GeoPoint pt = gpx_data.Crossroads.Single();
            Assert.AreEqual(38.8446733513524, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.7756934500661, pt.Longitude.Degrees, precision);
        }

        [TestMethod]
        public void TJuctionTest()
        {
            const string filename = @"Data/t-junction.gpx";

            IPlanData gpx_data = GpxLoader.ReadGpx(filename, Length.FromMeters(100), onProgress: null, CancellationToken.None);
            Assert.IsTrue(GpxLoader.tryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            Assert.AreEqual(1, gpx_data.Crossroads.Count);

            GeoPoint pt = gpx_data.Crossroads.Single();
            Assert.AreEqual(38.795438090984, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.80688648895, pt.Longitude.Degrees, precision);


        }

        private bool containsPoints(IEnumerable<ISegment> segments, List<List<GeoPoint>> tracks)
        {
            var segmented = new HashSet<GeoPoint>(segments.SelectMany(it => it.Points()));
            return segmented.IsSupersetOf(tracks.SelectMany(it => it));
        }
    }
}