namespace TCalc.Web.Services;

/// <summary>
/// Computes descriptive statistics for numeric data sets.
/// </summary>
public sealed class StatisticsService : IStatisticsService
{
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(ILogger<StatisticsService> logger) => _logger = logger;

    public StatisticsResult Compute(double[] data)
    {
        if (data is null || data.Length == 0)
            return StatisticsResult.Fail("Data set is empty.");

        if (data.Any(d => double.IsNaN(d) || double.IsInfinity(d)))
            return StatisticsResult.Fail("Data contains invalid values (NaN or Infinity).");

        _logger.LogDebug("Computing descriptive statistics for {Count} data points", data.Length);

        int n = data.Length;
        double[] sorted = [.. data.OrderBy(x => x)];

        double sum = data.Sum();
        double mean = sum / n;
        double min = sorted[0];
        double max = sorted[^1];

        double median = ComputePercentile(sorted, 0.5);
        double q1 = ComputePercentile(sorted, 0.25);
        double q3 = ComputePercentile(sorted, 0.75);

        // Population variance and standard deviation
        double variance = data.Sum(x => (x - mean) * (x - mean)) / n;
        double stdDev = Math.Sqrt(variance);

        // Sample variance and standard deviation (Bessel's correction)
        double sampleVariance = n > 1 ? data.Sum(x => (x - mean) * (x - mean)) / (n - 1) : 0;
        double sampleStdDev = Math.Sqrt(sampleVariance);

        // Skewness (Fisher-Pearson, sample-adjusted)
        double skewness = 0;
        if (n > 2 && sampleStdDev > 0)
        {
            double m3 = data.Sum(x => Math.Pow(x - mean, 3)) / n;
            skewness = m3 / Math.Pow(stdDev, 3);
        }

        // Excess kurtosis (population, Fisher definition)
        double kurtosis = 0;
        if (n > 3 && variance > 0)
        {
            double m4 = data.Sum(x => Math.Pow(x - mean, 4)) / n;
            kurtosis = m4 / (variance * variance) - 3.0;
        }

        // Mode — values with the highest frequency
        double[] mode = ComputeMode(data);

        return new StatisticsResult
        {
            Success = true,
            Count = n,
            Sum = sum,
            Mean = mean,
            Median = median,
            Mode = mode,
            Min = min,
            Max = max,
            Range = max - min,
            Variance = variance,
            StdDev = stdDev,
            SampleVariance = sampleVariance,
            SampleStdDev = sampleStdDev,
            Q1 = q1,
            Q3 = q3,
            IQR = q3 - q1,
            Skewness = skewness,
            Kurtosis = kurtosis,
        };
    }

    /// <summary>
    /// Computes a percentile using linear interpolation between closest ranks.
    /// </summary>
    internal static double ComputePercentile(double[] sorted, double p)
    {
        if (sorted.Length == 1) return sorted[0];

        double rank = p * (sorted.Length - 1);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);
        if (lower == upper) return sorted[lower];

        double fraction = rank - lower;
        return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
    }

    /// <summary>
    /// Returns the value(s) that appear most frequently.
    /// Returns empty if all values appear the same number of times.
    /// </summary>
    internal static double[] ComputeMode(double[] data)
    {
        var freq = new Dictionary<double, int>();
        foreach (double v in data)
        {
            freq.TryGetValue(v, out int count);
            freq[v] = count + 1;
        }

        int maxFreq = freq.Values.Max();
        if (maxFreq == 1) return []; // no mode — all values unique

        double[] modes = freq.Where(kv => kv.Value == maxFreq)
                             .Select(kv => kv.Key)
                             .OrderBy(x => x)
                             .ToArray();

        // If every value has the same frequency, there's no meaningful mode
        if (modes.Length == freq.Count) return [];

        return modes;
    }
}
