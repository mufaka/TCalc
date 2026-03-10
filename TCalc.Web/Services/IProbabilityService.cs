namespace TCalc.Web.Services;

/// <summary>
/// Computes probability density/mass functions and cumulative distribution functions
/// for common probability distributions.
/// </summary>
public interface IProbabilityService
{
    /// <summary>
    /// Computes Normal (Gaussian) distribution values.
    /// </summary>
    NormalDistributionResult Normal(double mean, double stdDev, double? x = null);

    /// <summary>
    /// Computes Binomial distribution values.
    /// </summary>
    BinomialDistributionResult Binomial(int n, double p, int? k = null);

    /// <summary>
    /// Computes Poisson distribution values.
    /// </summary>
    PoissonDistributionResult Poisson(double lambda, int? k = null);
}

public sealed class DistributionPoint
{
    public double X { get; init; }
    public double Y { get; init; }
}

public sealed class NormalDistributionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double Mean { get; init; }
    public double StdDev { get; init; }

    /// <summary>PDF value at the queried x, if x was provided.</summary>
    public double? PdfAtX { get; init; }

    /// <summary>CDF value at the queried x (P(X ≤ x)), if x was provided.</summary>
    public double? CdfAtX { get; init; }

    /// <summary>Points for plotting the PDF curve.</summary>
    public DistributionPoint[] PdfCurve { get; init; } = [];

    public static NormalDistributionResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public sealed class BinomialDistributionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public int N { get; init; }
    public double P { get; init; }
    public double ExpectedValue { get; init; }
    public double Variance { get; init; }
    public double StdDev { get; init; }

    /// <summary>PMF value at the queried k, if k was provided.</summary>
    public double? PmfAtK { get; init; }

    /// <summary>CDF value at the queried k (P(X ≤ k)), if k was provided.</summary>
    public double? CdfAtK { get; init; }

    /// <summary>Points for plotting the PMF bars.</summary>
    public DistributionPoint[] PmfPoints { get; init; } = [];

    public static BinomialDistributionResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public sealed class PoissonDistributionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double Lambda { get; init; }
    public double ExpectedValue { get; init; }
    public double Variance { get; init; }
    public double StdDev { get; init; }

    /// <summary>PMF value at the queried k, if k was provided.</summary>
    public double? PmfAtK { get; init; }

    /// <summary>CDF value at the queried k (P(X ≤ k)), if k was provided.</summary>
    public double? CdfAtK { get; init; }

    /// <summary>Points for plotting the PMF bars.</summary>
    public DistributionPoint[] PmfPoints { get; init; } = [];

    public static PoissonDistributionResult Fail(string error) =>
        new() { Success = false, Error = error };
}
