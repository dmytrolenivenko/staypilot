using StayPilot.Application.Helpers.Calculators;

namespace StayPilot.UnitTests
{
    public class LeastSquaresTests
    {
        [Fact]
        public void Fit_DataOnAPerfectLine_RecoversTheInterceptAndSlope()
        {
            // y = 3 + 2x exactly, so the fit has one right answer and no excuse to miss it.
            var rows = new List<double[]>();
            var targets = new List<double>();

            for (var x = 0; x < 20; x++)
            {
                rows.Add(new double[] { 1, x });
                targets.Add(3 + 2 * x);
            }

            var fit = LeastSquares.Fit(rows, targets);

            Assert.Equal(3, fit.Coefficients[0], precision: 6);
            Assert.Equal(2, fit.Coefficients[1], precision: 6);
        }

        [Fact]
        public void Fit_TwoInputsBothMattering_SeparatesTheirEffects()
        {
            // y = 1 + 5a - 2b. The two inputs vary independently, so a correct fit must pull
            // them apart rather than smear one's effect onto the other.
            var rows = new List<double[]>();
            var targets = new List<double>();

            for (var a = 0; a < 6; a++)
            {
                for (var b = 0; b < 6; b++)
                {
                    rows.Add(new double[] { 1, a, b });
                    targets.Add(1 + 5 * a - 2 * b);
                }
            }

            var fit = LeastSquares.Fit(rows, targets);

            Assert.Equal(5, fit.Coefficients[1], precision: 5);
            Assert.Equal(-2, fit.Coefficients[2], precision: 5);
        }

        [Fact]
        public void Fit_PerfectFit_LeavesNoResidualVariance()
        {
            var rows = new List<double[]>();
            var targets = new List<double>();

            for (var x = 0; x < 15; x++)
            {
                rows.Add(new double[] { 1, x });
                targets.Add(4 * x);
            }

            var fit = LeastSquares.Fit(rows, targets);

            Assert.True(fit.ResidualVariance < 1e-12, $"expected no leftover error, got {fit.ResidualVariance}");

            // No leftover error means no uncertainty, so the confidence margin collapses too.
            Assert.Equal(0, fit.ConfidenceMargin(1), precision: 6);
        }

        [Fact]
        public void Fit_NoisyData_ProducesANonZeroConfidenceMargin()
        {
            // Same underlying line, but jittered. The margin has to grow to admit the doubt.
            var random = new Random(4242);
            var rows = new List<double[]>();
            var targets = new List<double>();

            for (var x = 0; x < 200; x++)
            {
                rows.Add(new double[] { 1, x });
                targets.Add(2 * x + (random.NextDouble() - 0.5) * 40);
            }

            var fit = LeastSquares.Fit(rows, targets);

            Assert.True(fit.ConfidenceMargin(1) > 0, "noisy data must not report perfect certainty");

            // The true slope of 2 should still sit inside the range it reports.
            var margin = fit.ConfidenceMargin(1);
            Assert.InRange(2d, fit.Coefficients[1] - margin, fit.Coefficients[1] + margin);
        }

        [Fact]
        public void Fit_NoRows_Throws()
        {
            Assert.Throws<ArgumentException>(() => LeastSquares.Fit(new List<double[]>(), new List<double>()));
        }

        [Fact]
        public void Fit_TargetCountDoesNotMatchRowCount_Throws()
        {
            var rows = new List<double[]> { new double[] { 1, 1 }, new double[] { 1, 2 } };

            Assert.Throws<ArgumentException>(() => LeastSquares.Fit(rows, new List<double> { 1 }));
        }

        [Fact]
        public void Fit_FewerRowsThanColumns_Throws()
        {
            // Two rows cannot pin down three unknowns; fitting anyway would invent an answer.
            var rows = new List<double[]>
            {
                new double[] { 1, 1, 1 },
                new double[] { 1, 2, 4 }
            };

            Assert.Throws<ArgumentException>(() => LeastSquares.Fit(rows, new List<double> { 1, 2 }));
        }

        [Fact]
        public void Fit_ColumnThatNeverVaries_StillFitsTheUsefulColumns()
        {
            // A dummy column stuck at zero carries no information and makes the matrix
            // singular. The fit must survive it rather than blowing up, and still get the
            // real slope right.
            var rows = new List<double[]>();
            var targets = new List<double>();

            for (var x = 0; x < 20; x++)
            {
                rows.Add(new double[] { 1, x, 0 });
                targets.Add(7 + 3 * x);
            }

            var fit = LeastSquares.Fit(rows, targets);

            Assert.Equal(3, fit.Coefficients[1], precision: 5);
            Assert.Equal(0, fit.Coefficients[2], precision: 5);
        }
    }
}
