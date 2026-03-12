namespace TCalc.Web.Services;

/// <summary>
/// Provides methods for statistical inference: Bayesian updating,
/// hypothesis testing, maximum likelihood estimation, and confidence intervals.
/// </summary>
public interface IInferenceService
{
    /// <summary>
    /// Performs a Bayesian update using a Beta prior and binomial likelihood.
    /// Returns posterior parameters and curve data for visualization.
    /// </summary>
    BayesianUpdateResult BayesianUpdate(double priorAlpha, double priorBeta, int successes, int trials);

    /// <summary>
    /// Performs a one-sample z-test (known population σ).
    /// </summary>
    ZTestResult ZTest(double sampleMean, double populationMean, double populationStdDev, int sampleSize, double alpha, string alternative);

    /// <summary>
    /// Performs a one-sample t-test (unknown population σ, uses sample std dev).
    /// </summary>
    TTestResult TTest(double sampleMean, double populationMean, double sampleStdDev, int sampleSize, double alpha, string alternative);

    /// <summary>
    /// Performs a chi-square goodness-of-fit test.
    /// </summary>
    ChiSquareTestResult ChiSquareTest(double[] observed, double[] expected, double alpha);

    /// <summary>
    /// Computes MLE for a Normal distribution given observed data.
    /// Returns estimated μ, σ, and log-likelihood surface data.
    /// </summary>
    MleNormalResult MleNormal(double[] data);

    /// <summary>
    /// Computes MLE for a Binomial distribution (estimate p given n and observed successes).
    /// Returns estimated p and log-likelihood curve data.
    /// </summary>
    MleBinomialResult MleBinomial(int trials, int successes);

    /// <summary>
    /// Computes a confidence interval for a population mean.
    /// Uses z-interval when population σ is known, t-interval otherwise.
    /// </summary>
    ConfidenceIntervalResult ConfidenceInterval(double sampleMean, double stdDev, int sampleSize, double confidenceLevel, bool isPopulationStdDev);
}

// ── Bayesian Update ─────────────────────────────────────────────────────

public sealed class BayesianUpdateResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double PriorAlpha { get; init; }
    public double PriorBeta { get; init; }
    public double PosteriorAlpha { get; init; }
    public double PosteriorBeta { get; init; }
    public double PosteriorMean { get; init; }

    /// <summary>95% credible interval lower bound.</summary>
    public double CredibleLower { get; init; }

    /// <summary>95% credible interval upper bound.</summary>
    public double CredibleUpper { get; init; }

    /// <summary>Prior Beta PDF curve points over θ ∈ [0, 1].</summary>
    public DistributionPoint[] PriorCurve { get; init; } = [];

    /// <summary>Likelihood curve points over θ ∈ [0, 1] (proportional).</summary>
    public DistributionPoint[] LikelihoodCurve { get; init; } = [];

    /// <summary>Posterior Beta PDF curve points over θ ∈ [0, 1].</summary>
    public DistributionPoint[] PosteriorCurve { get; init; } = [];

    public static BayesianUpdateResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Z-Test ──────────────────────────────────────────────────────────────

public sealed class ZTestResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double TestStatistic { get; init; }
    public double PValue { get; init; }
    public double CriticalValue { get; init; }
    public bool RejectNull { get; init; }
    public string Alternative { get; init; } = "";
    public double Alpha { get; init; }

    /// <summary>Standard normal PDF curve points for visualization.</summary>
    public DistributionPoint[] DistributionCurve { get; init; } = [];

    public static ZTestResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── T-Test ──────────────────────────────────────────────────────────────

public sealed class TTestResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double TestStatistic { get; init; }
    public double PValue { get; init; }
    public double CriticalValue { get; init; }
    public bool RejectNull { get; init; }
    public string Alternative { get; init; } = "";
    public double Alpha { get; init; }
    public int DegreesOfFreedom { get; init; }

    /// <summary>T-distribution PDF curve points for visualization.</summary>
    public DistributionPoint[] DistributionCurve { get; init; } = [];

    public static TTestResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Chi-Square Test ─────────────────────────────────────────────────────

public sealed class ChiSquareTestResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double TestStatistic { get; init; }
    public double PValue { get; init; }
    public double CriticalValue { get; init; }
    public bool RejectNull { get; init; }
    public double Alpha { get; init; }
    public int DegreesOfFreedom { get; init; }

    public static ChiSquareTestResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── MLE Normal ──────────────────────────────────────────────────────────

public sealed class MleNormalResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double EstimatedMean { get; init; }
    public double EstimatedStdDev { get; init; }
    public double LogLikelihood { get; init; }

    /// <summary>Log-likelihood values over a grid of (μ, σ) for contour visualization.</summary>
    public MleContourPoint[] ContourData { get; init; } = [];

    /// <summary>μ axis values for the contour grid.</summary>
    public double[] MuAxis { get; init; } = [];

    /// <summary>σ axis values for the contour grid.</summary>
    public double[] SigmaAxis { get; init; } = [];

    public static MleNormalResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public sealed class MleContourPoint
{
    public double Mu { get; init; }
    public double Sigma { get; init; }
    public double LogLikelihood { get; init; }
}

// ── MLE Binomial ────────────────────────────────────────────────────────

public sealed class MleBinomialResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public int Trials { get; init; }
    public int Successes { get; init; }
    public double EstimatedP { get; init; }
    public double LogLikelihood { get; init; }

    /// <summary>Log-likelihood curve over p ∈ (0, 1) for visualization.</summary>
    public DistributionPoint[] LogLikelihoodCurve { get; init; } = [];

    public static MleBinomialResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Confidence Interval ─────────────────────────────────────────────────

public sealed class ConfidenceIntervalResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double SampleMean { get; init; }
    public double StdDev { get; init; }
    public int SampleSize { get; init; }
    public double ConfidenceLevel { get; init; }
    public double MarginOfError { get; init; }
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
    public double CriticalValue { get; init; }
    public string IntervalType { get; init; } = "";

    public static ConfidenceIntervalResult Fail(string error) =>
        new() { Success = false, Error = error };
}
