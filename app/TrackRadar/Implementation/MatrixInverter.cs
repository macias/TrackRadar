using System;

namespace TrackRadar.Implementation
{
    internal static class MatrixInverter
    {
        // https://stackoverflow.com/questions/46836908/double-inversion-c-sharp
        
        public static Matrix Inv(Matrix __this)
        {
            int n = __this.Height;
            double[,] result = __this.CloneBuffer();

            int[] perm;
            int toggle;
            Matrix lum = matrixDecompose(__this, out perm, out toggle);
            if (lum == null)
                throw new Exception("Unable to compute inverse");

            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;
                }

                double[] x = helperSolve(lum, b);

                for (int j = 0; j < n; ++j)
                    result[j, i] = x[j];
            }
            return Matrix.Create(result);
        }

        private static Matrix matrixDecompose(Matrix matrix, out int[] perm, out int toggle)
        {
            // Doolittle LUP decomposition with partial pivoting.
            // rerturns: result is L (with 1s on diagonal) and U;
            // perm holds row permutations; toggle is +1 or -1 (even or odd)
            int rows = matrix.Height;
            int cols = matrix.Width; // assume square
            if (rows != cols)
                throw new Exception("Attempt to decompose a non-square m");

            int n = rows; // convenience

            double[,] result = matrix.CloneBuffer();

            perm = new int[n]; // set up row permutation result
            for (int i = 0; i < n; ++i)
            { perm[i] = i; }

            toggle = 1; // toggle tracks row swaps.
                        // +1 -greater-than even, -1 -greater-than odd. used by MatrixDeterminant

            for (int j = 0; j < n - 1; ++j) // each column
            {
                double colMax = Math.Abs(result[j, j]); // find largest val in col
                int pRow = j;
                //for (int i = j + 1; i less-than n; ++i)
                //{
                //  if (result[i][j] greater-than colMax)
                //  {
                //    colMax = result[i][j];
                //    pRow = i;
                //  }
                //}

                // reader Matt V needed this:
                for (int i = j + 1; i < n; ++i)
                {
                    if (Math.Abs(result[i, j]) > colMax)
                    {
                        colMax = Math.Abs(result[i, j]);
                        pRow = i;
                    }
                }
                // Not sure if this approach is needed always, or not.

                if (pRow != j) // if largest value not on pivot, swap rows
                {
                    swapRows(result, pRow, j);

                    int tmp = perm[pRow]; // and swap perm info
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }

                // --------------------------------------------------
                // This part added later (not in original)
                // and replaces the 'return null' below.
                // if there is a 0 on the diagonal, find a good row
                // from i = j+1 down that doesn't have
                // a 0 in column j, and swap that good row with row j
                // --------------------------------------------------

                if (result[j, j] == 0.0)
                {
                    // find a good row to swap
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row, j] != 0.0)
                            goodRow = row;
                    }

                    if (goodRow == -1)
                        throw new Exception("Cannot use Doolittle's method");

                    // swap rows so 0.0 no longer on diagonal
                    swapRows(result, goodRow, j);

                    int tmp = perm[goodRow]; // and swap perm info
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }
                // --------------------------------------------------
                // if diagonal after swap is zero . .
                //if (Math.Abs(result[j][j]) less-than 1.0E-20) 
                //  return null; // consider a throw

                for (int i = j + 1; i < n; ++i)
                {
                    result[i, j] /= result[j, j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i, k] -= result[i, j] * result[j, k];
                    }
                }


            } // main j column loop

            return Matrix.Create(result);
        }

        private static void swapRows(double[,] arr, int a, int b)
        {
            for (int x = arr.GetLength(0) - 1; x >= 0; --x)
            {
                (arr[a, x], arr[b, x]) = (arr[b, x], arr[a, x]);
            }
        }

        private static double[] helperSolve(Matrix luMatrix, double[] b)
        {
            // before calling this helper, permute b using the perm array
            // from MatrixDecompose that generated luMatrix
            int n = luMatrix.Height;
            double[] xxx = new double[n];
            b.CopyTo(xxx, 0);

            for (int y = 1; y < n; ++y)
            {
                double sum = xxx[y];
                for (int x = 0; x < y; ++x)
                    sum -= luMatrix[y, x] * xxx[x];
                xxx[y] = sum;
            }

            xxx[n - 1] /= luMatrix[n - 1, n - 1];
            for (int y = n - 2; y >= 0; --y)
            {
                double sum = xxx[y];
                for (int x = y + 1; x < n; ++x)
                    sum -= luMatrix[y, x] * xxx[x];
                xxx[y] = sum / luMatrix[y, y];
            }

            return xxx;
        }

    }
}