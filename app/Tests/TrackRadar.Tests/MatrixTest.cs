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
        public void MultiplicationTest()
        {
            {
                var a = Matrix.Create(5, 5, 4, 7, 2, 6, 34, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                    20, 21, 22, 23, 24, 25);
                var b = Matrix.Create(5, 5, 4, 7, 2, 6, 34, 6, -7, 8, 9, 10, 11, 12, 13, 14, 15, -16, 17, 18, -19,
                    -70, 21, 22, -23, 24, 2);

                var ab = a * b;
                var ab_t = ab.T();

                var b_t_a_t = b.T() * a.T();
                AssertEqual(ab_t, b_t_a_t, precision);
            }
            {
                var a = Matrix.Create(5, 5, 4, 7, 2, 6, 34, 61, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                    20, 21, 22, 23, 24, 25);
                var b = Matrix.Create(new double[,] { { -3.77302356024956E-17, 0.0181818181818182, -0.0858428030303032, 0.117140151515152, -0.0494791666666667 },
{ 0.194444444444444, -0.0294612794612795, 333599972397811, -667199944795626, 333599972397814 },
{ -0.420138888888889, 0.0078493265993266, -792299934444807, 1.58459986888962E+15, -792299934444810 },
{ 0.256944444444445, -4.20875420875412E-05, 583799951696176, -1.16759990339235E+15, 583799951696176 },
{ -0.03125, 0.00347222222222222, -125099989649180, 250199979298361, -125099989649180 }});

                var ab = a * b;

                Console.WriteLine(ab);

                var ab_t = ab.T();

                var b_t_a_t = b.T() * a.T();
                AssertEqual(ab_t, b_t_a_t, precision);
            }

        }

        [TestMethod]
        public void AdditionTest()
        {
            var a = Matrix.Create(5, 5, 4, 7, 2, 6, 34, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                20, 21, 22, 23, 24, 25);
            var b = Matrix.Create(5, 5, 4, 7, 2, 6, 34, 6, -7, 8, 9, 10, 11, 12, 13, 14, 15, -16, 17, 18, -19,
                -70, 21, 22, -23, 24, 2);

            var ab = a + b;
            var ab_t = ab.T();

            var a_t_b_t = a.T() + b.T();
            AssertEqual(ab_t, a_t_b_t, precision);
        }

//        [TestMethod]
        public void InverseTest()
        {
            /* {
                 var m = Matrix.Create(2, 2, 8, 5, 13, 8);
                 var inv = m.Inv();
                 Assert.AreEqual(-8, inv[0, 0], precision);
                 Assert.AreEqual(5, inv[0, 1], precision);
                 Assert.AreEqual(13, inv[1, 0], precision);
                 Assert.AreEqual(-8, inv[1, 1], precision);

                 AssertEqual(Matrix.Identity(2), m * inv, precision);
                 AssertEqual(Matrix.Identity(2), inv * m, precision);
             }
             {
                 var m = Matrix.Create(2, 2, 7, 4, 3, 2);
                 var inv = m.Inv();
                 Assert.AreEqual(1, inv[0, 0], precision);
                 Assert.AreEqual(-2, inv[0, 1], precision);
                 Assert.AreEqual(-1.5, inv[1, 0], precision);
                 Assert.AreEqual(3.5, inv[1, 1], precision);

                 AssertEqual(Matrix.Identity(2), m * inv, precision);
                 AssertEqual(Matrix.Identity(2), inv * m, precision);
             }
             for (int n = 1; n < 10; ++n)
             {
                 var m = Matrix.Identity(n);
                 var inv = m.Inv();

                 AssertEqual(m, inv, precision);
             }

             {
                 var m = Matrix.Create(2, 2, 4, 7, 2, 6);
                 var inv = m.Inv();
                 Assert.AreEqual(0.6, inv[0, 0], precision);
                 Assert.AreEqual(-0.7, inv[0, 1], precision);
                 Assert.AreEqual(-0.2, inv[1, 0], precision);
                 Assert.AreEqual(0.4, inv[1, 1], precision);

                 AssertEqual(Matrix.Identity(2), m * inv, precision);
                 AssertEqual(Matrix.Identity(2), inv * m, precision);
             }
             {
                 var m = Matrix.Create(3, 3, 4, 7, 2, 6, 34, 6, 7, 8, 9);
                 var inv = m.Inv();

                 AssertEqual(Matrix.Identity(m.Width), m * inv, precision);
                 AssertEqual(Matrix.Identity(m.Width), inv * m, precision);
             }
             {
                 var m = Matrix.Create(3, 3, 4, 0, 2, 6, 0, 6, 7, 8, 9);
                 var inv = m.Inv();

                 AssertEqual(Matrix.Identity(m.Width), m * inv, precision);
                 AssertEqual(Matrix.Identity(m.Width), inv * m, precision);
             }
             {
                 var m = Matrix.Create(4, 4, 4, 7, 2, 6, 34, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
                 var inv = m.Inv();

                 AssertEqual(Matrix.Identity(m.Width), m * inv, precision);
                 AssertEqual(Matrix.Identity(m.Width), inv * m, precision);
             }*/
            {
                var m = Matrix.Create(5, 5,
                    4, 7, 2, 6, 34,
                    61, 7, 8, 9, 10,
                    11, 12, 13, 14, 15,
                    16, 17, 18, 19, 20,
                    21, 22, 23, 24, 25);
                // this should throw an exception
                var inv = m.Inv();

                AssertEqual(Matrix.Identity(m.Width), m * inv, precision);
                AssertEqual(Matrix.Identity(m.Width), inv * m, precision);
            }
            {
                var m = Matrix.Create(5, 5, 4, 7, 2, 6, 34, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                    20, 21, 22, 23, 24, 25);
                Assert.ThrowsException<ArgumentException>(m.Inv, "Matrix is not invertible.");
            }
            {
                var m = Matrix.Create(6, 6, 4, 7, 2, 6, 34, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                    20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36);
                var inv = m.Inv();

                AssertEqual(Matrix.Identity(m.Width), m * inv, precision);
                AssertEqual(Matrix.Identity(m.Width), inv * m, precision);
            }
        }

        private static void AssertEqual(Matrix a, Matrix b, double prec)
        {
            Assert.AreEqual(a.Width, b.Width);
            Assert.AreEqual(a.Height, b.Height);

            for (int y = 0; y < a.Height; ++y)
                for (int x = 0; x < a.Width; ++x)
                    Assert.AreEqual(a[y, x], b[y, x], prec);
        }


    }
}