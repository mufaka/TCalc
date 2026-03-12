using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class TimeSeriesServiceTests
{
    private readonly TimeSeriesService _service = new(NullLogger<TimeSeriesService>.Instance);

    // ════════════════════════════════════════════════════════════════
    //  Decomposition
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Decompose_KnownSeries_ReturnsComponents()
    {
        // 12 data points with period 4: clear trend + seasonal pattern
        double[] values = [10, 15, 12, 8, 12, 17, 14, 10, 14, 19, 16, 12];

        var result = _service.Decompose(values, seasonalPeriod: 4);

        Assert.True(result.Success);
        Assert.Equal(12, result.Original.Length);
        Assert.Equal(12, result.Trend.Length);
        Assert.Equal(12, result.Seasonal.Length);
        Assert.Equal(12, result.Residual.Length);
        Assert.Equal(4, result.SeasonalPeriod);
    }

    [Fact]
    public void Decompose_TrendComponentSmooths()
    {
        // Linear trend with period-4 seasonality
        double[] values = [2, 4, 3, 1, 4, 6, 5, 3, 6, 8, 7, 5];

        var result = _service.Decompose(values, seasonalPeriod: 4);

        Assert.True(result.Success);

        // Trend should exist for middle values (not edges)
        Assert.Null(result.Trend[0]);
        Assert.Null(result.Trend[1]);
        Assert.NotNull(result.Trend[4]);
        Assert.NotNull(result.Trend[7]);
        Assert.Null(result.Trend[10]);
        Assert.Null(result.Trend[11]);
    }

    [Fact]
    public void Decompose_SeasonalComponentRepeats()
    {
        double[] values = [10, 15, 12, 8, 12, 17, 14, 10, 14, 19, 16, 12];

        var result = _service.Decompose(values, seasonalPeriod: 4);

        Assert.True(result.Success);

        // Seasonal component should repeat with period 4
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(result.Seasonal[i]!.Value, result.Seasonal[i + 4]!.Value, 10);
        }
    }

    [Fact]
    public void Decompose_SeasonalComponentSumsToZero()
    {
        double[] values = [10, 15, 12, 8, 12, 17, 14, 10, 14, 19, 16, 12];

        var result = _service.Decompose(values, seasonalPeriod: 4);

        Assert.True(result.Success);

        // One full seasonal cycle should sum to ~0
        double sum = 0;
        for (int j = 0; j < 4; j++)
            sum += result.Seasonal[j]!.Value;
        Assert.InRange(sum, -0.01, 0.01);
    }

    [Fact]
    public void Decompose_ResidualIsSmallForCleanData()
    {
        // Perfect linear trend + exact seasonal: residual should be near zero
        double[] values = new double[12];
        for (int i = 0; i < 12; i++)
            values[i] = 10 + i * 2 + new[] { 3.0, -1.0, 2.0, -4.0 }[i % 4];

        var result = _service.Decompose(values, seasonalPeriod: 4);

        Assert.True(result.Success);

        foreach (var r in result.Residual)
        {
            if (r.HasValue)
                Assert.InRange(r.Value, -2.0, 2.0);
        }
    }

    [Fact]
    public void Decompose_TooFewPoints_ReturnsError()
    {
        var result = _service.Decompose([1, 2, 3], 2);
        Assert.False(result.Success);
    }

    [Fact]
    public void Decompose_PeriodLessThan2_ReturnsError()
    {
        var result = _service.Decompose([1, 2, 3, 4, 5, 6], 1);
        Assert.False(result.Success);
    }

    [Fact]
    public void Decompose_TooFewPointsForPeriod_ReturnsError()
    {
        var result = _service.Decompose([1, 2, 3, 4, 5], seasonalPeriod: 4);
        Assert.False(result.Success);
        Assert.Contains("8", result.Error!); // needs at least 2×period = 8
    }

    [Fact]
    public void Decompose_NullValues_ReturnsError()
    {
        var result = _service.Decompose(null!, 4);
        Assert.False(result.Success);
    }

    [Fact]
    public void Decompose_OddPeriod_Succeeds()
    {
        // 15 points with period 3
        double[] values = [1, 3, 2, 2, 4, 3, 3, 5, 4, 4, 6, 5, 5, 7, 6];

        var result = _service.Decompose(values, seasonalPeriod: 3);

        Assert.True(result.Success);
        Assert.Equal(3, result.SeasonalPeriod);
    }

    // ════════════════════════════════════════════════════════════════
    //  Autocorrelation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Autocorrelation_Lag0_IsOne()
    {
        double[] values = [1, 3, 2, 5, 4, 7, 6, 9, 8];

        var result = _service.Autocorrelation(values, maxLag: 5);

        Assert.True(result.Success);
        Assert.Equal(1.0, result.Values[0], 10);
    }

    [Fact]
    public void Autocorrelation_WhiteNoise_NearZero()
    {
        // Pseudo-random-ish values — autocorrelation at high lags should be small
        var rng = new Random(42);
        double[] values = Enumerable.Range(0, 200).Select(_ => rng.NextDouble()).ToArray();

        var result = _service.Autocorrelation(values, maxLag: 20);

        Assert.True(result.Success);

        // Lags 1–20 should be within the significance band (approximately)
        for (int i = 1; i <= 20; i++)
        {
            Assert.InRange(Math.Abs(result.Values[i]), 0, 0.25);
        }
    }

    [Fact]
    public void Autocorrelation_StrongTrend_HighLag1()
    {
        // Linear trend → high positive autocorrelation at lag 1
        double[] values = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();

        var result = _service.Autocorrelation(values, maxLag: 5);

        Assert.True(result.Success);
        Assert.True(result.Values[1] > 0.9);
    }

    [Fact]
    public void Autocorrelation_ReturnsCorrectLagCount()
    {
        double[] values = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        var result = _service.Autocorrelation(values, maxLag: 5);

        Assert.True(result.Success);
        Assert.Equal(6, result.Lags.Length);   // 0, 1, 2, 3, 4, 5
        Assert.Equal(6, result.Values.Length);
    }

    [Fact]
    public void Autocorrelation_SignificanceBandIsPositive()
    {
        double[] values = [1, 2, 3, 4, 5];

        var result = _service.Autocorrelation(values, maxLag: 2);

        Assert.True(result.Success);
        Assert.True(result.SignificanceBand > 0);
        // 1.96 / √5 ≈ 0.877
        Assert.InRange(result.SignificanceBand, 0.87, 0.88);
    }

    [Fact]
    public void Autocorrelation_MaxLagExceedsN_ClampedToN()
    {
        double[] values = [1, 2, 3, 4, 5];

        var result = _service.Autocorrelation(values, maxLag: 100);

        Assert.True(result.Success);
        Assert.Equal(5, result.Lags.Length); // clamped to n-1=4, plus lag 0
    }

    [Fact]
    public void Autocorrelation_ConstantSeries_ReturnsError()
    {
        var result = _service.Autocorrelation([5, 5, 5, 5, 5], 3);
        Assert.False(result.Success);
        Assert.Contains("variance", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Autocorrelation_TooFewPoints_ReturnsError()
    {
        var result = _service.Autocorrelation([1, 2], 1);
        Assert.False(result.Success);
    }

    [Fact]
    public void Autocorrelation_InvalidMaxLag_ReturnsError()
    {
        var result = _service.Autocorrelation([1, 2, 3, 4], 0);
        Assert.False(result.Success);
    }

    // ════════════════════════════════════════════════════════════════
    //  Forecast (Simple Exponential Smoothing)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForecastSimple_ReturnsCorrectHorizon()
    {
        double[] values = [10, 12, 14, 16, 18, 20];

        var result = _service.ForecastSimple(values, horizon: 5);

        Assert.True(result.Success);
        Assert.Equal(5, result.Forecast.Length);
        Assert.Equal(5, result.UpperBound.Length);
        Assert.Equal(5, result.LowerBound.Length);
    }

    [Fact]
    public void ForecastSimple_FlatForecastAtLastSmoothedValue()
    {
        double[] values = [10, 10, 10, 10, 10];

        var result = _service.ForecastSimple(values, horizon: 3);

        Assert.True(result.Success);
        // Constant series → forecast = constant
        foreach (var f in result.Forecast)
            Assert.Equal(10.0, f, 5);
    }

    [Fact]
    public void ForecastSimple_UpperBoundsAboveForecast()
    {
        double[] values = [5, 10, 8, 12, 9, 14, 11];

        var result = _service.ForecastSimple(values, horizon: 3);

        Assert.True(result.Success);
        for (int i = 0; i < 3; i++)
        {
            Assert.True(result.UpperBound[i] >= result.Forecast[i]);
            Assert.True(result.LowerBound[i] <= result.Forecast[i]);
        }
    }

    [Fact]
    public void ForecastSimple_PredictionIntervalWidensWithHorizon()
    {
        double[] values = [5, 10, 8, 12, 9, 14, 11, 16];

        var result = _service.ForecastSimple(values, horizon: 5);

        Assert.True(result.Success);
        for (int i = 1; i < 5; i++)
        {
            double prevWidth = result.UpperBound[i - 1] - result.LowerBound[i - 1];
            double curWidth = result.UpperBound[i] - result.LowerBound[i];
            Assert.True(curWidth >= prevWidth);
        }
    }

    [Fact]
    public void ForecastSimple_PreservesHistorical()
    {
        double[] values = [1, 2, 3, 4, 5];

        var result = _service.ForecastSimple(values, horizon: 2);

        Assert.True(result.Success);
        Assert.Equal(values, result.Historical);
    }

    [Fact]
    public void ForecastSimple_AlphaStored()
    {
        var result = _service.ForecastSimple([1, 2, 3, 4], horizon: 1, alpha: 0.5);

        Assert.True(result.Success);
        Assert.Equal(0.5, result.Alpha);
    }

    [Fact]
    public void ForecastSimple_TooFewPoints_ReturnsError()
    {
        var result = _service.ForecastSimple([1], horizon: 1);
        Assert.False(result.Success);
    }

    [Fact]
    public void ForecastSimple_InvalidHorizon_ReturnsError()
    {
        Assert.False(_service.ForecastSimple([1, 2, 3], horizon: 0).Success);
        Assert.False(_service.ForecastSimple([1, 2, 3], horizon: 101).Success);
    }

    [Fact]
    public void ForecastSimple_InvalidAlpha_ReturnsError()
    {
        Assert.False(_service.ForecastSimple([1, 2, 3], horizon: 1, alpha: 0).Success);
        Assert.False(_service.ForecastSimple([1, 2, 3], horizon: 1, alpha: 1).Success);
    }

    // ════════════════════════════════════════════════════════════════
    //  PCA 2-D
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Pca2D_CorrelatedData_PC1ExplainsMoreVariance()
    {
        // Strongly correlated: y ≈ x
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] y = [1.1, 2.0, 3.1, 3.9, 5.2, 5.8, 7.1, 8.0, 8.9, 10.2];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);
        Assert.True(result.ExplainedVariance1 > 0.95);
        Assert.True(result.ExplainedVariance2 < 0.05);
    }

    [Fact]
    public void Pca2D_ExplainedVarianceSumsToOne()
    {
        double[] x = [1, 2, 3, 4, 5];
        double[] y = [2, 4, 5, 4, 5];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);
        Assert.InRange(result.ExplainedVariance1 + result.ExplainedVariance2, 0.999, 1.001);
    }

    [Fact]
    public void Pca2D_EigenvectorsAreOrthogonal()
    {
        double[] x = [1, 3, 5, 7, 9];
        double[] y = [2, 3, 6, 8, 9];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);

        double dot = result.Pc1X * result.Pc2X + result.Pc1Y * result.Pc2Y;
        Assert.InRange(dot, -0.001, 0.001); // orthogonal
    }

    [Fact]
    public void Pca2D_EigenvectorsAreUnitLength()
    {
        double[] x = [1, 3, 5, 7, 9];
        double[] y = [2, 3, 6, 8, 9];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);

        double norm1 = Math.Sqrt(result.Pc1X * result.Pc1X + result.Pc1Y * result.Pc1Y);
        double norm2 = Math.Sqrt(result.Pc2X * result.Pc2X + result.Pc2Y * result.Pc2Y);
        Assert.InRange(norm1, 0.999, 1.001);
        Assert.InRange(norm2, 0.999, 1.001);
    }

    [Fact]
    public void Pca2D_MeanCentredData_HasZeroMean()
    {
        double[] x = [2, 4, 6, 8];
        double[] y = [1, 3, 5, 7];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);

        double centredXMean = result.CentredX.Average();
        double centredYMean = result.CentredY.Average();
        Assert.InRange(centredXMean, -0.001, 0.001);
        Assert.InRange(centredYMean, -0.001, 0.001);
    }

    [Fact]
    public void Pca2D_ProjectedPointsOnPC1()
    {
        double[] x = [1, 2, 3, 4, 5];
        double[] y = [1, 2, 3, 4, 5];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);
        Assert.Equal(5, result.ProjectedX.Length);
        Assert.Equal(5, result.ProjectedY.Length);
    }

    [Fact]
    public void Pca2D_UncorrelatedData_EqualEigenvalues()
    {
        // Axis-aligned data with equal spread
        double[] x = [1, -1, 0, 0];
        double[] y = [0, 0, 1, -1];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);
        // Both eigenvalues should be similar
        Assert.InRange(result.Eigenvalue1 / (result.Eigenvalue2 + 1e-15), 0.5, 2.0);
    }

    [Fact]
    public void Pca2D_TooFewPoints_ReturnsError()
    {
        var result = _service.Pca2D([1, 2], [3, 4]);
        Assert.False(result.Success);
    }

    [Fact]
    public void Pca2D_MismatchedLengths_ReturnsError()
    {
        var result = _service.Pca2D([1, 2, 3], [4, 5]);
        Assert.False(result.Success);
    }

    [Fact]
    public void Pca2D_NullInputs_ReturnsError()
    {
        Assert.False(_service.Pca2D(null!, [1, 2, 3]).Success);
        Assert.False(_service.Pca2D([1, 2, 3], null!).Success);
    }

    [Fact]
    public void Pca2D_EigenvaluesAreNonNegative()
    {
        double[] x = [1, 2, 3, 4, 5, 6];
        double[] y = [3, 1, 4, 1, 5, 9];

        var result = _service.Pca2D(x, y);

        Assert.True(result.Success);
        Assert.True(result.Eigenvalue1 >= 0);
        Assert.True(result.Eigenvalue2 >= 0);
        Assert.True(result.Eigenvalue1 >= result.Eigenvalue2); // PC1 is the larger
    }
}
