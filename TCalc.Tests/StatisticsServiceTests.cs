using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class StatisticsServiceTests
{
    private readonly StatisticsService _service = new(NullLogger<StatisticsService>.Instance);

    // ─── Basic descriptive stats ──────────────────────────────

    [Fact]
    public void Compute_BasicDataSet()
    {
        var result = _service.Compute([2, 4, 4, 4, 5, 5, 7, 9]);

        Assert.True(result.Success);
        Assert.Equal(8, result.Count);
        Assert.Equal(40, result.Sum);
        Assert.Equal(5, result.Mean);
        Assert.Equal(4.5, result.Median, 10);
        Assert.Equal(2, result.Min);
        Assert.Equal(9, result.Max);
        Assert.Equal(7, result.Range);
    }

    [Fact]
    public void Compute_Mode_SingleMode()
    {
        var result = _service.Compute([1, 2, 2, 3, 4]);

        Assert.True(result.Success);
        Assert.Single(result.Mode);
        Assert.Equal(2, result.Mode[0]);
    }

    [Fact]
    public void Compute_Mode_MultipleMode()
    {
        var result = _service.Compute([1, 1, 2, 2, 3]);

        Assert.True(result.Success);
        Assert.Equal(2, result.Mode.Length);
        Assert.Contains(1.0, result.Mode);
        Assert.Contains(2.0, result.Mode);
    }

    [Fact]
    public void Compute_Mode_NoMode_AllUnique()
    {
        var result = _service.Compute([1, 2, 3, 4, 5]);

        Assert.True(result.Success);
        Assert.Empty(result.Mode);
    }

    [Fact]
    public void Compute_Mode_NoMode_AllSameFrequency()
    {
        var result = _service.Compute([1, 1, 2, 2, 3, 3]);

        Assert.True(result.Success);
        Assert.Empty(result.Mode);
    }

    // ─── Variance & Standard Deviation ────────────────────────

    [Fact]
    public void Compute_Variance()
    {
        // Population variance of [2, 4, 4, 4, 5, 5, 7, 9] with mean=5
        // Sum of squared deviations: 9+1+1+1+0+0+4+16 = 32, variance = 32/8 = 4
        var result = _service.Compute([2, 4, 4, 4, 5, 5, 7, 9]);

        Assert.True(result.Success);
        Assert.Equal(4.0, result.Variance, 10);
        Assert.Equal(2.0, result.StdDev, 10);
    }

    [Fact]
    public void Compute_SampleVariance()
    {
        // Sample variance: 32/7 ≈ 4.571428
        var result = _service.Compute([2, 4, 4, 4, 5, 5, 7, 9]);

        Assert.True(result.Success);
        Assert.Equal(32.0 / 7.0, result.SampleVariance, 10);
        Assert.Equal(Math.Sqrt(32.0 / 7.0), result.SampleStdDev, 10);
    }

    // ─── Quartiles & IQR ──────────────────────────────────────

    [Fact]
    public void Compute_Quartiles()
    {
        var result = _service.Compute([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        Assert.True(result.Success);
        Assert.Equal(3.25, result.Q1, 10);  // 25th percentile
        Assert.Equal(5.5, result.Median, 10);
        Assert.Equal(7.75, result.Q3, 10);   // 75th percentile
        Assert.Equal(4.5, result.IQR, 10);
    }

    // ─── Edge cases ───────────────────────────────────────────

    [Fact]
    public void Compute_SingleValue()
    {
        var result = _service.Compute([42]);

        Assert.True(result.Success);
        Assert.Equal(1, result.Count);
        Assert.Equal(42, result.Mean);
        Assert.Equal(42, result.Median);
        Assert.Equal(0, result.Variance);
        Assert.Equal(0, result.Range);
    }

    [Fact]
    public void Compute_AllIdentical()
    {
        var result = _service.Compute([5, 5, 5, 5, 5]);

        Assert.True(result.Success);
        Assert.Equal(5, result.Mean);
        Assert.Equal(5, result.Median);
        Assert.Equal(0, result.Variance);
        Assert.Equal(0, result.StdDev);
        Assert.Equal(0, result.Range);
    }

    [Fact]
    public void Compute_EmptyArray_ReturnsFail()
    {
        var result = _service.Compute([]);

        Assert.False(result.Success);
        Assert.Contains("empty", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_NullArray_ReturnsFail()
    {
        var result = _service.Compute(null!);

        Assert.False(result.Success);
    }

    [Fact]
    public void Compute_ContainsNaN_ReturnsFail()
    {
        var result = _service.Compute([1, double.NaN, 3]);

        Assert.False(result.Success);
        Assert.Contains("NaN", result.Error!);
    }

    [Fact]
    public void Compute_ContainsInfinity_ReturnsFail()
    {
        var result = _service.Compute([1, double.PositiveInfinity, 3]);

        Assert.False(result.Success);
        Assert.Contains("Infinity", result.Error!);
    }

    [Fact]
    public void Compute_TwoValues()
    {
        var result = _service.Compute([10, 20]);

        Assert.True(result.Success);
        Assert.Equal(15, result.Mean);
        Assert.Equal(15, result.Median);
        Assert.Equal(10, result.Range);
    }

    [Fact]
    public void Compute_NegativeValues()
    {
        var result = _service.Compute([-5, -3, -1, 1, 3, 5]);

        Assert.True(result.Success);
        Assert.Equal(0, result.Mean, 10);
        Assert.Equal(0, result.Median, 10);
        Assert.Equal(-5, result.Min);
        Assert.Equal(5, result.Max);
    }

    // ─── Skewness & Kurtosis ──────────────────────────────────

    [Fact]
    public void Compute_SymmetricData_SkewnessNearZero()
    {
        // Symmetric data should have skewness near 0
        var result = _service.Compute([1, 2, 3, 4, 5, 6, 7, 8, 9]);

        Assert.True(result.Success);
        Assert.True(Math.Abs(result.Skewness) < 0.01);
    }

    [Fact]
    public void Compute_NormalLike_KurtosisNearZero()
    {
        // Uniform distribution has negative excess kurtosis (≈ -1.2)
        var result = _service.Compute([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        Assert.True(result.Success);
        Assert.True(result.Kurtosis < 0); // platykurtic
    }

    // ─── Percentile helper ────────────────────────────────────

    [Fact]
    public void ComputePercentile_Interpolation()
    {
        double[] sorted = [1, 2, 3, 4, 5];
        Assert.Equal(1, StatisticsService.ComputePercentile(sorted, 0));
        Assert.Equal(3, StatisticsService.ComputePercentile(sorted, 0.5));
        Assert.Equal(5, StatisticsService.ComputePercentile(sorted, 1.0));
    }

    [Fact]
    public void ComputePercentile_SingleElement()
    {
        double[] sorted = [42];
        Assert.Equal(42, StatisticsService.ComputePercentile(sorted, 0.5));
    }
}
