using System;
using System.Threading;

namespace TrackRadar.Implementation
{
    internal sealed class MatrixInverter2
    {
        /*private readonly struct Fraction
        {
            public static readonly Fraction _1 = new Fraction(1, 1);
            public readonly double Numerator;
            public readonly double Denominator;

            public Fraction(double num,double den)
            {
                this.Numerator = num;
                this.Denominator = den;
            }

            public static Fraction operator/(Fraction a,Fraction b)
            {
                return new Fraction(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
            }
            public static Fraction operator *(Fraction a, Fraction b)
            {
                return new Fraction(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
            }
            public static Fraction operator +(Fraction a, Fraction b)
            {
                return new Fraction(a.Numerator * b.Denominator+b.Numerator*a.Denominator, a.Denominator * b.Denominator);
            }
        }
        private static Fraction[,] identity(int n)
        {
            var result = new Fraction[n, n];
            for (int i = 0; i < n; ++i)
                result[i, i] = Fraction._1;
            return result;
        }
        */
        public static Matrix Inv(Matrix m)
        {
            if (m.Width != m.Height)
                throw new ArgumentException();

            m = m.Clone();
            Matrix inv = Matrix.Identity(m.Width);

            for (int i = 0; i < m.Height; ++i)
            {
                if (m[i, i] == 0)
                {
                    bool found = false;
                    for (int y = i + 1; y < m.Height && !found; ++y)
                        if (m[y, i] != 0)
                        {
                            addScaledRow(y, i, 1, m);
                            addScaledRow(y, i, 1, inv);
                            found = true;
                        }

                    if (!found)
                        throw new ArgumentException("Matrix is not invertible.");
                }

                for (int y = 0; y < m.Height; ++y)
                    if (i != y)
                    {
                        Console.WriteLine($"Subtracting from row {y}");
                        double factor = -m[y, i]/m[i,i];
                        addScaledRow(i, y, factor, inv);
                        addScaledRow(i, y, factor, m);
                        Console.WriteLine(m);
                    }

                if (m[i, i] != 1)
                {
                    Console.WriteLine($"Scaling row {i} to 1");
                    double factor = m[i, i];
                    divideRow(i, factor, inv);
                    divideRow(i, factor, m);
                    Console.WriteLine(m);
                }



                Console.WriteLine();
            }

            Console.WriteLine(m);
            Console.WriteLine();
            Console.WriteLine(inv);
            return inv;
        }

        private static void divideRow(int y, double factor, Matrix m)
        {
            for (int x = 0; x < m.Width; ++x)
                m[y, x] /= factor;
        }

        private static void addScaledRow(int srcY, int dstY, double factor, Matrix m)
        {
            for (int x = 0; x < m.Width; ++x)
                m[dstY, x] += m[srcY, x] * factor;
        }

       /* private static void divideRow(int y, Fraction factor, Fraction[,] m)
        {
            for (int x = 0; x < m.GetLength(1); ++x)
                m[y, x] /= factor;
        }

        private static void addScaledRow(int srcY, int dstY, Fraction factor, Fraction[,] m)
        {
            for (int x = 0; x < m.GetLength(1); ++x)
                m[dstY, x] += m[srcY, x] * factor;
        }*/
    }
}