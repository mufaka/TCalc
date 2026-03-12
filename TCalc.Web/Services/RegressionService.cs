namespace TCalc.Web.Services;

/// <summary>
/// Computes linear and polynomial regression using the normal equations (least squares).
/// </summary>
public sealed class RegressionService : IRegressionService
{
    private readonly ILogger<RegressionService> _logger;

    public RegressionService(ILogger<RegressionService> logger) => _logger = logger;

    public RegressionResult LinearRegression(double[] x, double[] y)
    {
        _logger.LogDebug("Performing linear regression on {Count} data points", x?.Length ?? 0);
        if (x is null || y is null || x.Length != y.Length)
            return RegressionResult.Fail("X and Y arrays must be non-null and equal length.");

        if (x.Length < 2)
            return RegressionResult.Fail("At least 2 data points are required for linear regression.");

        return PolynomialRegression(x, y, degree: 1);
    }

    public RegressionResult PolynomialRegression(double[] x, double[] y, int degree)
    {
        _logger.LogDebug("Performing degree-{Degree} polynomial regression on {Count} data points", degree, x?.Length ?? 0);
        if (x is null || y is null || x.Length != y.Length)
            return RegressionResult.Fail("X and Y arrays must be non-null and equal length.");

        int n = x.Length;
        if (n < degree + 1)
            return RegressionResult.Fail($"At least {degree + 1} data points are required for degree-{degree} regression.");

        if (degree < 1 || degree > 10)
            return RegressionResult.Fail("Degree must be between 1 and 10.");

        // Build the Vandermonde matrix and solve via normal equations: (Xᵀ X) c = Xᵀ y
        int cols = degree + 1;
        double[,] xtx = new double[cols, cols];
        double[] xty = new double[cols];

        for (int i = 0; i < n; i++)
        {
            double[] powers = new double[cols];
            powers[0] = 1.0;
            for (int j = 1; j < cols; j++)
                powers[j] = powers[j - 1] * x[i];

            for (int r = 0; r < cols; r++)
            {
                for (int c = 0; c < cols; c++)
                    xtx[r, c] += powers[r] * powers[c];
                xty[r] += powers[r] * y[i];
            }
        }

        // Solve via Gaussian elimination with partial pivoting
        double[]? coefficients = SolveLinearSystem(xtx, xty);
        if (coefficients is null)
            return RegressionResult.Fail("Regression failed — singular matrix (data may be collinear).");

        // R² calculation
        double yMean = y.Average();
        double ssTot = y.Sum(yi => (yi - yMean) * (yi - yMean));
        double ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double predicted = EvaluatePolynomial(coefficients, x[i]);
            ssRes += (y[i] - predicted) * (y[i] - predicted);
        }
        double rSquared = ssTot > 0 ? 1.0 - ssRes / ssTot : (ssRes == 0 ? 1.0 : 0.0);

        // Generate fitted points for smooth trend line
        double xMin = x.Min();
        double xMax = x.Max();
        double step = (xMax - xMin) / 200.0;
        if (step <= 0) step = 1;

        var fitted = new List<(double X, double Y)>();
        for (double xi = xMin; xi <= xMax + step / 2; xi += step)
        {
            fitted.Add((xi, EvaluatePolynomial(coefficients, xi)));
        }

        // Build equation string
        string equation = FormatEquation(coefficients);

        return new RegressionResult
        {
            Success = true,
            Equation = equation,
            Coefficients = coefficients,
            RSquared = rSquared,
            FittedPoints = [.. fitted],
        };
    }

    /// <summary>
    /// Evaluates c[0] + c[1]x + c[2]x² + …
    /// </summary>
    internal static double EvaluatePolynomial(double[] coefficients, double x)
    {
        double result = 0;
        double power = 1;
        for (int i = 0; i < coefficients.Length; i++)
        {
            result += coefficients[i] * power;
            power *= x;
        }
        return result;
    }

    /// <summary>
    /// Formats polynomial coefficients into a human-readable equation.
    /// </summary>
    internal static string FormatEquation(double[] c)
    {
        if (c.Length == 2)
        {
            // Linear: y = mx + b
            return $"y = {c[1]:G6}x + {c[0]:G6}";
        }

        var parts = new List<string>();
        for (int i = c.Length - 1; i >= 0; i--)
        {
            double coeff = c[i];
            if (Math.Abs(coeff) < 1e-12) continue;

            string term = i switch
            {
                0 => $"{coeff:G6}",
                1 => $"{coeff:G6}x",
                _ => $"{coeff:G6}x^{i}"
            };
            parts.Add(term);
        }

        return parts.Count > 0 ? $"y = {string.Join(" + ", parts)}" : "y = 0";
    }

    /// <summary>
    /// Solves Ax = b via Gaussian elimination with partial pivoting.
    /// Returns null if the system is singular.
    /// </summary>
    internal static double[]? SolveLinearSystem(double[,] a, double[] b)
    {
        int n = b.Length;
        // Augment
        double[,] aug = new double[n, n + 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                aug[i, j] = a[i, j];
            aug[i, n] = b[i];
        }

        for (int col = 0; col < n; col++)
        {
            // Partial pivot
            int maxRow = col;
            double maxVal = Math.Abs(aug[col, col]);
            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(aug[row, col]) > maxVal)
                {
                    maxVal = Math.Abs(aug[row, col]);
                    maxRow = row;
                }
            }
            if (maxVal < 1e-14) return null; // singular

            if (maxRow != col)
            {
                for (int j = 0; j <= n; j++)
                    (aug[col, j], aug[maxRow, j]) = (aug[maxRow, j], aug[col, j]);
            }

            // Eliminate
            for (int row = col + 1; row < n; row++)
            {
                double factor = aug[row, col] / aug[col, col];
                for (int j = col; j <= n; j++)
                    aug[row, j] -= factor * aug[col, j];
            }
        }

        // Back substitution
        double[] result = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            if (Math.Abs(aug[i, i]) < 1e-14) return null;
            result[i] = aug[i, n];
            for (int j = i + 1; j < n; j++)
                result[i] -= aug[i, j] * result[j];
            result[i] /= aug[i, i];
        }

        return result;
    }

    // ── Multiple Linear Regression ──────────────────────────────────────

    public MultipleRegressionResult MultipleLinearRegression(double[][] X, double[] y)
    {
        _logger.LogDebug("Multiple linear regression: {Rows} observations, {Cols} predictors",
            X?.Length ?? 0, X?.Length > 0 ? X[0].Length : 0);

        if (X is null || y is null || X.Length == 0 || y.Length == 0)
            return MultipleRegressionResult.Fail("X and Y data are required.");
        if (X.Length != y.Length)
            return MultipleRegressionResult.Fail($"X has {X.Length} observations but Y has {y.Length}.");

        int n = X.Length;
        int k = X[0].Length; // number of predictors
        int p = k + 1; // total parameters (including intercept)

        if (n < p + 1)
            return MultipleRegressionResult.Fail($"Need at least {p + 1} observations for {k} predictors.");

        // Build design matrix with intercept column: [1, x1, x2, ..., xk]
        double[,] xtx = new double[p, p];
        double[] xty = new double[p];

        for (int i = 0; i < n; i++)
        {
            if (X[i].Length != k)
                return MultipleRegressionResult.Fail("All rows in X must have the same number of columns.");

            double[] row = new double[p];
            row[0] = 1.0; // intercept
            for (int j = 0; j < k; j++)
                row[j + 1] = X[i][j];

            for (int r = 0; r < p; r++)
            {
                for (int c = 0; c < p; c++)
                    xtx[r, c] += row[r] * row[c];
                xty[r] += row[r] * y[i];
            }
        }

        double[]? coefficients = SolveLinearSystem(xtx, xty);
        if (coefficients is null)
            return MultipleRegressionResult.Fail("Regression failed — singular matrix (predictors may be collinear).");

        // Compute fitted values and residuals
        double yMean = y.Average();
        double ssTot = 0, ssRes = 0;
        var fitted = new double[n];
        var residuals = new double[n];

        for (int i = 0; i < n; i++)
        {
            double pred = coefficients[0];
            for (int j = 0; j < k; j++)
                pred += coefficients[j + 1] * X[i][j];
            fitted[i] = pred;
            residuals[i] = y[i] - pred;
            ssRes += residuals[i] * residuals[i];
            ssTot += (y[i] - yMean) * (y[i] - yMean);
        }

        double rSquared = ssTot > 0 ? 1.0 - ssRes / ssTot : (ssRes == 0 ? 1.0 : 0.0);
        double adjustedR2 = 1.0 - (1.0 - rSquared) * (n - 1.0) / (n - p);

        // Standard errors of coefficients
        double mse = ssRes / (n - p);
        var standardErrors = new double[p];
        var tStats = new double[p];

        // Invert XᵀX for variance of coefficients
        double[,] xtxInv = InvertMatrix(xtx, p);
        if (xtxInv is not null)
        {
            for (int j = 0; j < p; j++)
            {
                double se = Math.Sqrt(Math.Max(0, mse * xtxInv[j, j]));
                standardErrors[j] = se;
                tStats[j] = se > 1e-15 ? coefficients[j] / se : 0;
            }
        }

        // Equation string
        var parts = new List<string> { $"ŷ = {coefficients[0]:G6}" };
        for (int j = 0; j < k; j++)
        {
            string sign = coefficients[j + 1] >= 0 ? " + " : " − ";
            parts.Add($"{sign}{Math.Abs(coefficients[j + 1]):G6}·x{j + 1}");
        }

        return new MultipleRegressionResult
        {
            Success = true,
            Coefficients = coefficients,
            RSquared = rSquared,
            AdjustedRSquared = adjustedR2,
            StandardErrors = standardErrors,
            TStatistics = tStats,
            Equation = string.Concat(parts),
            FittedValues = fitted,
            Residuals = residuals,
        };
    }

    // ── Logistic Regression (IRLS) ──────────────────────────────────────

    public LogisticRegressionResult LogisticRegression(double[] x, bool[] y)
    {
        _logger.LogDebug("Logistic regression on {N} observations", x?.Length ?? 0);

        if (x is null || y is null || x.Length != y.Length)
            return LogisticRegressionResult.Fail("X and Y arrays must be non-null and equal length.");
        if (x.Length < 3)
            return LogisticRegressionResult.Fail("At least 3 data points are required.");

        int n = x.Length;
        double yNumTrue = y.Count(v => v);
        if (yNumTrue == 0 || yNumTrue == n)
            return LogisticRegressionResult.Fail("Y must contain both true and false values.");

        // IRLS (Iteratively Reweighted Least Squares)
        double b0 = 0, b1 = 0;
        const int maxIter = 100;
        const double tol = 1e-8;

        for (int iter = 0; iter < maxIter; iter++)
        {
            // Compute predicted probabilities
            double[] p = new double[n];
            for (int i = 0; i < n; i++)
            {
                double z = b0 + b1 * x[i];
                p[i] = Sigmoid(z);
            }

            // Weight matrix diagonal and working response
            double sw00 = 0, sw01 = 0, sw11 = 0;
            double swz0 = 0, swz1 = 0;

            for (int i = 0; i < n; i++)
            {
                double w = p[i] * (1 - p[i]);
                if (w < 1e-12) w = 1e-12;

                double yNum = y[i] ? 1.0 : 0.0;
                double zWorking = b0 + b1 * x[i] + (yNum - p[i]) / w;

                sw00 += w;
                sw01 += w * x[i];
                sw11 += w * x[i] * x[i];
                swz0 += w * zWorking;
                swz1 += w * x[i] * zWorking;
            }

            // Solve 2×2 system
            double det = sw00 * sw11 - sw01 * sw01;
            if (Math.Abs(det) < 1e-15)
                return LogisticRegressionResult.Fail("Logistic regression failed — singular system.");

            double newB0 = (sw11 * swz0 - sw01 * swz1) / det;
            double newB1 = (sw00 * swz1 - sw01 * swz0) / det;

            if (Math.Abs(newB0 - b0) < tol && Math.Abs(newB1 - b1) < tol)
            {
                b0 = newB0;
                b1 = newB1;
                break;
            }

            b0 = newB0;
            b1 = newB1;
        }

        // Final predictions
        var probs = new double[n];
        int correct = 0;
        for (int i = 0; i < n; i++)
        {
            probs[i] = Sigmoid(b0 + b1 * x[i]);
            bool predicted = probs[i] >= 0.5;
            if (predicted == y[i]) correct++;
        }

        double accuracy = (double)correct / n;

        // Sigmoid curve for visualization
        double xMin = x.Min();
        double xMax = x.Max();
        double xPad = (xMax - xMin) * 0.2;
        xMin -= xPad;
        xMax += xPad;
        int steps = 100;
        var curve = new DistributionPoint[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            double xVal = xMin + (xMax - xMin) * i / steps;
            curve[i] = new DistributionPoint { X = xVal, Y = Sigmoid(b0 + b1 * xVal) };
        }

        return new LogisticRegressionResult
        {
            Success = true,
            Beta0 = b0,
            Beta1 = b1,
            PredictedProbabilities = probs,
            Accuracy = accuracy,
            SigmoidCurve = curve,
        };
    }

    private static double Sigmoid(double z)
    {
        if (z > 500) return 1.0;
        if (z < -500) return 0.0;
        return 1.0 / (1.0 + Math.Exp(-z));
    }

    /// <summary>
    /// Inverts a symmetric positive-definite matrix via Gaussian elimination.
    /// Returns null if singular.
    /// </summary>
    private static double[,]? InvertMatrix(double[,] a, int n)
    {
        double[,] aug = new double[n, 2 * n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                aug[i, j] = a[i, j];
            aug[i, n + i] = 1.0;
        }

        for (int col = 0; col < n; col++)
        {
            int maxRow = col;
            double maxVal = Math.Abs(aug[col, col]);
            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(aug[row, col]) > maxVal)
                {
                    maxVal = Math.Abs(aug[row, col]);
                    maxRow = row;
                }
            }
            if (maxVal < 1e-14) return null;

            if (maxRow != col)
            {
                for (int j = 0; j < 2 * n; j++)
                    (aug[col, j], aug[maxRow, j]) = (aug[maxRow, j], aug[col, j]);
            }

            double pivot = aug[col, col];
            for (int j = 0; j < 2 * n; j++)
                aug[col, j] /= pivot;

            for (int row = 0; row < n; row++)
            {
                if (row == col) continue;
                double factor = aug[row, col];
                for (int j = 0; j < 2 * n; j++)
                    aug[row, j] -= factor * aug[col, j];
            }
        }

        var inv = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                inv[i, j] = aug[i, n + j];

        return inv;
    }
}
