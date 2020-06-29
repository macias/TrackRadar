using Geo;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests
{

    [TestClass]
    public class MatrixTest
    {
        private const double precision = 0.00000001;

        [TestMethod]
        public void InverseTest()
        {
            var m = new Matrix(2, 2, 4, 7, 2, 6);
            var inv = m.Inv();
            Assert.AreEqual(0.6, inv[0, 0], precision);
            Assert.AreEqual(-0.7, inv[0, 1], precision);
            Assert.AreEqual(-0.2, inv[1, 0], precision);
            Assert.AreEqual(0.4, inv[1, 1], precision);

            AssertEqual(Matrix.Identity(2), m * inv, precision);
            AssertEqual(Matrix.Identity(2), inv * m, precision);
        }

        private static void AssertEqual(Matrix a, Matrix obj, double prec)
        {
            Assert.AreEqual(a.Width, obj.Width);
            Assert.AreEqual(a.Height, obj.Height);

            for (int y = 0; y < a.Height; ++y)
                for (int x = 0; x < a.Width; ++x)
                    Assert.AreEqual(a[y, x], obj[y, x], prec);
        }


    }
}