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

    /// <summary>
    /// Fits a multiple linear regression: y = β₀ + β₁x₁ + β₂x₂ + … + βₖxₖ.
    /// Each row of X is an observation; each column is a predictor variable.
    /// </summary>
    MultipleRegressionResult MultipleLinearRegression(double[][] X, double[] y);

    /// <summary>
    /// Fits a simple logistic regression: P(y=1|x) = 1/(1 + e^(−(β₀ + β₁x))).
    /// Uses iteratively reweighted least squares (IRLS).
    /// </summary>
    LogisticRegressionResult LogisticRegression(double[] x, bool[] y);
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

// ── Multiple Linear Regression ──────────────────────────────────────────

public sealed class MultipleRegressionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Coefficients: β₀ (intercept), β₁, β₂, …, βₖ.</summary>
    public double[] Coefficients { get; init; } = [];

    /// <summary>R² (coefficient of determination).</summary>
    public double RSquared { get; init; }

    /// <summary>Adjusted R² accounting for number of predictors.</summary>
    public double AdjustedRSquared { get; init; }

    /// <summary>Standard errors for each coefficient.</summary>
    public double[] StandardErrors { get; init; } = [];

    /// <summary>T-statistics for each coefficient.</summary>
    public double[] TStatistics { get; init; } = [];

    /// <summary>Human-readable equation.</summary>
    public string Equation { get; init; } = string.Empty;

    /// <summary>Predicted (fitted) values for each observation.</summary>
    public double[] FittedValues { get; init; } = [];

    /// <summary>Residuals (y − ŷ) for each observation.</summary>
    public double[] Residuals { get; init; } = [];

    public static MultipleRegressionResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Logistic Regression ─────────────────────────────────────────────────

public sealed class LogisticRegressionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Intercept β₀.</summary>
    public double Beta0 { get; init; }

    /// <summary>Slope β₁.</summary>
    public double Beta1 { get; init; }

    /// <summary>Predicted probabilities for each observation.</summary>
    public double[] PredictedProbabilities { get; init; } = [];

    /// <summary>Classification accuracy at threshold 0.5.</summary>
    public double Accuracy { get; init; }

    /// <summary>Sigmoid curve points (x, P(y=1|x)) for visualization.</summary>
    public DistributionPoint[] SigmoidCurve { get; init; } = [];

    public static LogisticRegressionResult Fail(string error) =>
        new() { Success = false, Error = error };
}
