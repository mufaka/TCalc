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
}
