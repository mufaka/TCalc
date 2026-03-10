namespace TCalc.Web.Services;

/// <summary>
/// Computes regression models for bivariate data sets.
/// </summary>
public interface IRegressionService
{
    /// <summary>
    /// Fits a linear regression (y = mx + b) to the given data points.
    /// </summary>
    RegressionResult LinearRegression(double[] x, double[] y);

    /// <summary>
    /// Fits a polynomial regression of the given degree to the data points.
    /// </summary>
    RegressionResult PolynomialRegression(double[] x, double[] y, int degree);
}

public sealed class RegressionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Human-readable equation string, e.g. "y = 2.50x + 1.00".</summary>
    public string Equation { get; init; } = string.Empty;

    /// <summary>Polynomial coefficients in ascending order: c[0] + c[1]x + c[2]x² + …</summary>
    public double[] Coefficients { get; init; } = [];

    /// <summary>Coefficient of determination (0–1).</summary>
    public double RSquared { get; init; }

    /// <summary>Fitted (x, y) points for plotting the trend line.</summary>
    public (double X, double Y)[] FittedPoints { get; init; } = [];

    public static RegressionResult Fail(string error) =>
        new() { Success = false, Error = error };
}
