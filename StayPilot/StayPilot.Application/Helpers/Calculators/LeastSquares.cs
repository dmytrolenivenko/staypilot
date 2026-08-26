namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// The result of fitting a least-squares line (well, hyperplane) through the data.
    /// </summary>
    internal class LeastSquaresFit
    {
        /// <summary>One number per input column: how much that column moves the answer.</summary>
        public double[] Coefficients { get; set; } = Array.Empty<double>();

        /// <summary>
        /// The inverted X'X matrix. Its diagonal, scaled by <see cref="ResidualVariance"/>,
        /// gives each coefficient's variance - which is where the confidence ranges come from.
        /// </summary>
        public double[,] InverseNormalMatrix { get; set; } = new double[0, 0];

        /// <summary>How much variation the fit could NOT explain, per degree of freedom.</summary>
        public double ResidualVariance { get; set; }

        /// <summary>
        /// The 95% confidence half-width for one coefficient, in the same units as the
        /// coefficient. 1.96 is the standard normal cutoff - with thousands of listings the
        /// t-distribution is close enough to normal that the difference is invisible.
        /// </summary>
        public double ConfidenceMargin(int column)
        {
            var variance = ResidualVariance * InverseNormalMatrix[column, column];

            return variance <= 0 ? 0 : 1.96 * Math.Sqrt(variance);
        }
    }

    /// <summary>
    /// Ordinary least squares, by hand. Kept in its own file so <see cref="FeaturePremiumCalculator"/>
    /// reads as "what a feature is worth", not "how to invert a matrix". No maths library: the
    /// whole thing is one normal-equations solve.
    /// </summary>
    internal static class LeastSquares
    {
        /// <summary>
        /// Added to the matrix diagonal before inverting, so an all-zero column can't make it
        /// uninvertible. Far too small to bias a real coefficient.
        /// </summary>
        private const double DiagonalGuard = 1e-7;

        /// <summary>
        /// Fits <paramref name="targets"/> from <paramref name="rows"/>, where each row holds one
        /// value per input column. Rows and targets must line up one-for-one.
        /// </summary>
        /// <exception cref="ArgumentException">No rows, mismatched lengths, or fewer rows than columns.</exception>
        public static LeastSquaresFit Fit(IReadOnlyList<double[]> rows, IReadOnlyList<double> targets)
        {
            if (rows.Count == 0)
                throw new ArgumentException("Cannot fit a model with no rows.", nameof(rows));

            if (rows.Count != targets.Count)
                throw new ArgumentException("Every row needs exactly one target value.", nameof(targets));

            var columns = rows[0].Length;

            if (rows.Count <= columns)
                throw new ArgumentException(
                    $"Need more rows ({rows.Count}) than columns ({columns}) to fit anything meaningful.", nameof(rows));

            return FitDense(rows, targets, columns);
        }

        /// <summary>
        /// One normal-equations matrix over every column, inverted whole.
        /// </summary>
        private static LeastSquaresFit FitDense(
            IReadOnlyList<double[]> rows, IReadOnlyList<double> targets, int columns)
        {
            // Normal equations: build X'X and X'y in one pass over the data.
            var normalMatrix = new double[columns, columns];
            var normalVector = new double[columns];

            for (var r = 0; r < rows.Count; r++)
            {
                AddRowToNormalEquations(rows[r], targets[r], normalMatrix, normalVector, columns);
            }

            for (var i = 0; i < columns; i++)
            {
                normalMatrix[i, i] += DiagonalGuard;
            }

            var inverse = Invert(normalMatrix, columns);

            // Coefficients = (X'X)^-1 X'y
            var coefficients = new double[columns];

            for (var i = 0; i < columns; i++)
            {
                var total = 0d;

                for (var j = 0; j < columns; j++)
                {
                    total += inverse[i, j] * normalVector[j];
                }

                coefficients[i] = total;
            }

            return new LeastSquaresFit
            {
                Coefficients = coefficients,
                InverseNormalMatrix = inverse,
                ResidualVariance = ResidualVariance(coefficients, rows, targets, columns),
            };
        }

        /// <summary>
        /// One row's contribution to X'X and X'y, skipping both sides of every zero product -
        /// most columns on most rows are dummy flags sitting at zero, and each is zero on either
        /// its own row-value or its partner's just as often. Purely an efficiency move: the sum
        /// it computes is identical to summing every i,j pair without skipping.
        /// </summary>
        private static void AddRowToNormalEquations(
            double[] row, double target, double[,] normalMatrix, double[] normalVector, int columns)
        {
            for (var i = 0; i < columns; i++)
            {
                var vi = row[i];

                if (vi == 0)
                    continue;

                normalVector[i] += vi * target;

                for (var j = 0; j < columns; j++)
                {
                    var vj = row[j];

                    if (vj == 0)
                        continue;

                    normalMatrix[i, j] += vi * vj;
                }
            }
        }

        /// <summary>Average leftover error, spread over the degrees of freedom.</summary>
        private static double ResidualVariance(
            double[] coefficients, IReadOnlyList<double[]> rows, IReadOnlyList<double> targets, int columns)
        {
            var sumSquaredResiduals = 0d;

            for (var r = 0; r < rows.Count; r++)
            {
                sumSquaredResiduals += Math.Pow(targets[r] - Predict(coefficients, rows[r]), 2);
            }

            return sumSquaredResiduals / (rows.Count - columns);
        }

        /// <summary>
        /// Applies a fitted set of coefficients to one row: just a dot product.
        /// </summary>
        public static double Predict(double[] coefficients, double[] row)
        {
            var total = 0d;

            for (var i = 0; i < coefficients.Length; i++)
            {
                total += coefficients[i] * row[i];
            }

            return total;
        }

        /// <summary>
        /// Inverts a square matrix by Gauss-Jordan elimination with partial pivoting
        /// (swap in the biggest available pivot each step, which keeps rounding error small).
        /// </summary>
        private static double[,] Invert(double[,] matrix, int size)
        {
            // Work on [matrix | identity]; when the left half becomes the identity, the
            // right half is the inverse.
            var work = new double[size, 2 * size];

            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    work[i, j] = matrix[i, j];
                }

                work[i, size + i] = 1;
            }

            for (var column = 0; column < size; column++)
            {
                var pivotRow = column;

                for (var r = column + 1; r < size; r++)
                {
                    if (Math.Abs(work[r, column]) > Math.Abs(work[pivotRow, column]))
                        pivotRow = r;
                }

                if (pivotRow != column)
                {
                    for (var j = 0; j < 2 * size; j++)
                    {
                        (work[column, j], work[pivotRow, j]) = (work[pivotRow, j], work[column, j]);
                    }
                }

                var pivot = work[column, column];

                // Still no usable pivot after the swap -> this column is redundant. Leave its
                // row alone; the coefficient comes out as zero, which is the honest answer.
                if (Math.Abs(pivot) < 1e-12)
                    continue;

                for (var j = 0; j < 2 * size; j++)
                {
                    work[column, j] /= pivot;
                }

                for (var r = 0; r < size; r++)
                {
                    if (r == column)
                        continue;

                    var factor = work[r, column];

                    if (factor == 0)
                        continue;

                    for (var j = 0; j < 2 * size; j++)
                    {
                        work[r, j] -= factor * work[column, j];
                    }
                }
            }

            var inverse = new double[size, size];

            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    inverse[i, j] = work[i, size + j];
                }
            }

            return inverse;
        }
    }
}
