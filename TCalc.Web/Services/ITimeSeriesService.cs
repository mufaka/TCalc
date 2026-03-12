namespace TCalc.Web.Services;

/// <summary>
/// Provides methods for time series analysis: decomposition, autocorrelation, and forecasting.
/// </summary>
public interface ITimeSeriesService
{
    /// <summary>
    /// Decomposes a time series into trend, seasonal, and residual components
    /// using moving-average decomposition.
    /// </summary>
    DecompositionResult Decompose(double[] values, int seasonalPeriod);

    /// <summary>
    /// Computes the autocorrelation function (ACF) for a time series at multiple lags.
    /// </summary>
    AutocorrelationResult Autocorrelation(double[] values, int maxLag);

    /// <summary>
    /// Produces a simple forecast using exponential smoothing.
    /// </summary>
    ForecastResult ForecastSimple(double[] values, int horizon, double alpha = 0.3);

    /// <summary>
    /// Computes 2-D Principal Component Analysis on a set of (x, y) points.
    /// Returns eigenvectors, eigenvalues, projected points, and explained variance.
    /// </summary>
    PcaResult Pca2D(double[] x, double[] y);
}

// ── Decomposition ───────────────────────────────────────────────────────

public sealed class DecompositionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Original series values.</summary>
    public double[] Original { get; init; } = [];

    /// <summary>Trend component (moving average).</summary>
    public double?[] Trend { get; init; } = [];

    /// <summary>Seasonal component (additive).</summary>
    public double?[] Seasonal { get; init; } = [];

    /// <summary>Residual = Original − Trend − Seasonal.</summary>
    public double?[] Residual { get; init; } = [];

    public int SeasonalPeriod { get; init; }

    public static DecompositionResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Autocorrelation ─────────────────────────────────────────────────────

public sealed class AutocorrelationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Lag values (0, 1, 2, …, maxLag).</summary>
    public int[] Lags { get; init; } = [];

    /// <summary>Autocorrelation at each lag.</summary>
    public double[] Values { get; init; } = [];

    /// <summary>Approximate 95% significance band = ±1.96/√n.</summary>
    public double SignificanceBand { get; init; }

    public static AutocorrelationResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Forecast ────────────────────────────────────────────────────────────

public sealed class ForecastResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Original values (for plotting).</summary>
    public double[] Historical { get; init; } = [];

    /// <summary>Forecasted values beyond the series.</summary>
    public double[] Forecast { get; init; } = [];

    /// <summary>Upper bound of approximate 95% prediction interval.</summary>
    public double[] UpperBound { get; init; } = [];

    /// <summary>Lower bound of approximate 95% prediction interval.</summary>
    public double[] LowerBound { get; init; } = [];

    /// <summary>Smoothing parameter used.</summary>
    public double Alpha { get; init; }

    public static ForecastResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── PCA 2-D ─────────────────────────────────────────────────────────────

public sealed class PcaResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Mean-centred X values.</summary>
    public double[] CentredX { get; init; } = [];

    /// <summary>Mean-centred Y values.</summary>
    public double[] CentredY { get; init; } = [];

    /// <summary>Centroid (meanX, meanY).</summary>
    public double MeanX { get; init; }
    public double MeanY { get; init; }

    /// <summary>First principal component direction (unit vector).</summary>
    public double Pc1X { get; init; }
    public double Pc1Y { get; init; }

    /// <summary>Second principal component direction (unit vector).</summary>
    public double Pc2X { get; init; }
    public double Pc2Y { get; init; }

    /// <summary>Eigenvalue for PC1.</summary>
    public double Eigenvalue1 { get; init; }

    /// <summary>Eigenvalue for PC2.</summary>
    public double Eigenvalue2 { get; init; }

    /// <summary>Explained variance ratio for PC1 (0–1).</summary>
    public double ExplainedVariance1 { get; init; }

    /// <summary>Explained variance ratio for PC2 (0–1).</summary>
    public double ExplainedVariance2 { get; init; }

    /// <summary>Projected X values onto PC1 (for visualization).</summary>
    public double[] ProjectedX { get; init; } = [];

    /// <summary>Projected Y values onto PC1 (for visualization).</summary>
    public double[] ProjectedY { get; init; } = [];

    public static PcaResult Fail(string error) =>
        new() { Success = false, Error = error };
}
