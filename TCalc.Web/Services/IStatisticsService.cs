namespace TCalc.Web.Services;

/// <summary>
/// Computes descriptive statistics for numeric data sets.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Computes all descriptive statistics for the given data.
    /// </summary>
    StatisticsResult Compute(double[] data);
}

public sealed class StatisticsResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public int Count { get; init; }
    public double Sum { get; init; }
    public double Mean { get; init; }
    public double Median { get; init; }
    public double[] Mode { get; init; } = [];
    public double Min { get; init; }
    public double Max { get; init; }
    public double Range { get; init; }
    public double Variance { get; init; }
    public double StdDev { get; init; }
    public double SampleVariance { get; init; }
    public double SampleStdDev { get; init; }
    public double Q1 { get; init; }
    public double Q3 { get; init; }
    public double IQR { get; init; }
    public double Skewness { get; init; }
    public double Kurtosis { get; init; }

    public static StatisticsResult Fail(string error) =>
        new() { Success = false, Error = error };
}
