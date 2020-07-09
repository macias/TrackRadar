using Geo;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{

    [TestClass]
    public class KalmanTest
    {
        private const double precision = 0.00000001;

        [TestMethod]
        public void WRONG_StraightLineTest()
        {
            var stamper = new SecondStamper();
            var filter = new KalmanFilter(stamper);

            double est_x, est_y;
            filter.Compute(stamper.GetTimestamp(), 0, 0, 1,out est_x,out est_y);
            Assert.AreEqual(0, est_x, precision);
            Assert.AreEqual(0, est_y, precision);

            stamper.Advance();
            filter.Compute(stamper.GetTimestamp(), 1, 0, 1, out est_x, out est_y);
            Assert.AreEqual(0.5, est_x,precision);
            Assert.AreEqual(0, est_y, precision);

            stamper.Advance();
            filter.Compute(stamper.GetTimestamp(), 2, 0, 1, out est_x, out est_y);
            Assert.AreEqual(2, est_x, precision);
            Assert.AreEqual(0, est_y, precision);

            stamper.Advance();
            filter.Compute(stamper.GetTimestamp(), 3, 0, 1, out est_x, out est_y);
            Assert.AreEqual(5.52941176470588, est_x, precision);
            Assert.AreEqual(0, est_y, precision);

            stamper.Advance();
            filter.Compute(stamper.GetTimestamp(), 4, 0, 1, out est_x, out est_y);
            Assert.AreEqual(13.1881537834533, est_x, precision);
            Assert.AreEqual(0, est_y, precision);

            stamper.Advance();
            filter.Compute(stamper.GetTimestamp(), 5, 0, 1, out est_x, out est_y);
            Assert.AreEqual(29.2070587505045, est_x, precision);
            Assert.AreEqual(0, est_y, precision);

            stamper.Advance();
            filter.Compute(stamper.GetTimestamp(), 6, 0, 1, out est_x, out est_y);
            Assert.AreEqual(62.0493783010743, est_x, precision);
            Assert.AreEqual(0, est_y, precision);

            stamper.Advance();
            filter.Compute(stamper.GetTimestamp(), 7, 0, 1, out est_x, out est_y);
            Assert.AreEqual(128.618878643089, est_x, precision);
            Assert.AreEqual(0, est_y, precision);
        }

    }
}