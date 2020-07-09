using System;
using System.Text;

namespace TrackRadar.Implementation
{
    internal sealed class Matrix
    {
        public static Matrix Identity(int n)
        {
            // return an n x n Identity matrix
            var result = new double[n, n];
            for (int i = 0; i < n; ++i)
                result[i, i] = 1.0;

            return new Matrix(result);
        }

        public static Matrix Create(int height, int width, params double[] values)
        {
            var data = new double[height, width];
            if (values.Length != 0)
            {
                if (values.Length != height * width)
                    throw new ArgumentException("Data mismatch error");
                for (int y = 0; y < height; ++y)
                    for (int x = 0; x < width; ++x)
                        data[y, x] = values[y * width + x];
            }

            return new Matrix(data);
        }

        public static Matrix Create(double[,] values)
        {
            return new Matrix(values);
        }

        public static Matrix Diagonal(params double[] values)
        {
            var result = new double[values.Length, values.Length];
            for (int i = 0; i < values.Length; ++i)
                result[i, i] = values[i];

            return new Matrix(result);
        }

        private readonly double[,] data;
        public int Height => data.GetLength(0);
        public int Width => data.GetLength(1);

        public double this[int y,int x] { get { return this.data[y, x]; } set { this.data[y, x] = value; } }

        private Matrix(double[,] values)
        {
            this.data = values;
        }

        public double[,] CloneBuffer()
        {
            return this.data.Clone() as double[,];
        }

        public Matrix Clone()
        {
            return new Matrix(this.CloneBuffer());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Matrix);
        }

        public bool Equals(Matrix obj)
        {
            if (Object.ReferenceEquals(null, obj))
                return false;
            if (Object.ReferenceEquals(this, obj))
                return false;

            if (this.Width != obj.Width || this.Height != obj.Height)
                return false;

            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    if (this[y, x] != obj[y, x])
                        return false;

            return true;
        }

        public Matrix Inv()
        {
            //  return MatrixInverter.Inv(this);
            return MatrixInverter2.Inv(this);
        }

        public Matrix T()
        {
            var res = new double[Width, Height];
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                {
                    res[x, y] = this.data[y, x];
                }

            return new Matrix(res);
        }

        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a.Width != b.Height)
                throw new InvalidOperationException();

            var res = new double[a.Height, b.Width];
            for (int y = 0; y < a.Height; ++y)
                for (int x = 0; x < b.Width; ++x)
                {
                    double sum = 0;
                    for (int i = 0; i < a.Width; ++i)
                        sum += a.data[y, i] * b.data[i, x];
                    res[y, x] = sum;
                }

            return new Matrix(res);
        }

        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
                throw new InvalidOperationException();

            var res = new double[a.Height, a.Width];
            for (int y = 0; y < a.Height; ++y)
                for (int x = 0; x < a.Width; ++x)
                {
                    res[y, x] = a.data[y, x] + b.data[y, x];
                }

            return new Matrix(res);
        }

        public static Matrix operator -(Matrix a, Matrix b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
                throw new InvalidOperationException();

            var res = new double[a.Height, a.Width];
            for (int y = 0; y < a.Height; ++y)
                for (int x = 0; x < a.Width; ++x)
                {
                    res[y, x] = a.data[y, x] + b.data[y, x];
                }

            return new Matrix(res);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int y = 0; y < Height; ++y)
            {
                if (y > 0)
                    sb.Append("," + Environment.NewLine);
                int x = 0;
                sb.Append("{ " + this[y, x]);
                for (++x; x < Width; ++x)
                    sb.Append(", " + this[y, x]);
                sb.Append(" }");
            }

            return sb.ToString();
        }
    }
}