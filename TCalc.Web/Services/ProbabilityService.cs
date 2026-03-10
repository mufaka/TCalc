namespace TCalc.Web.Services;

/// <summary>
/// Pure-math implementation of common probability distributions.
/// </summary>
public sealed class ProbabilityService : IProbabilityService
{
    private readonly ILogger<ProbabilityService> _logger;

    public ProbabilityService(ILogger<ProbabilityService> logger) => _logger = logger;

    // ────────────────────────────────────────────────────────────
    //  Normal (Gaussian) Distribution
    // ────────────────────────────────────────────────────────────

    public NormalDistributionResult Normal(double mean, double stdDev, double? x = null)
    {
        _logger.LogDebug("Computing Normal distribution: mean={Mean}, stdDev={StdDev}, x={X}", mean, stdDev, x);
        if (stdDev <= 0)
            return NormalDistributionResult.Fail("Standard deviation must be positive.");
        if (double.IsNaN(mean) || double.IsInfinity(mean))
            return NormalDistributionResult.Fail("Mean must be a finite number.");
        if (double.IsNaN(stdDev) || double.IsInfinity(stdDev))
            return NormalDistributionResult.Fail("Standard deviation must be a finite number.");

        double? pdfAtX = x.HasValue ? NormalPdf(x.Value, mean, stdDev) : null;
        double? cdfAtX = x.HasValue ? NormalCdf(x.Value, mean, stdDev) : null;

        // Generate PDF curve: mean ± 4σ with 200 points
        const int curvePoints = 200;
        double lo = mean - 4 * stdDev;
        double hi = mean + 4 * stdDev;
        double step = (hi - lo) / (curvePoints - 1);

        var curve = new DistributionPoint[curvePoints];
        for (int i = 0; i < curvePoints; i++)
        {
            double xi = lo + i * step;
            curve[i] = new DistributionPoint { X = Math.Round(xi, 6), Y = Math.Round(NormalPdf(xi, mean, stdDev), 8) };
        }

        return new NormalDistributionResult
        {
            Success = true,
            Mean = mean,
            StdDev = stdDev,
            PdfAtX = pdfAtX.HasValue ? Math.Round(pdfAtX.Value, 8) : null,
            CdfAtX = cdfAtX.HasValue ? Math.Round(cdfAtX.Value, 8) : null,
            PdfCurve = curve,
        };
    }

    internal static double NormalPdf(double x, double mean, double stdDev)
    {
        double z = (x - mean) / stdDev;
        return Math.Exp(-0.5 * z * z) / (stdDev * Math.Sqrt(2.0 * Math.PI));
    }

    /// <summary>
    /// CDF via the Abramowitz &amp; Stegun approximation of the error function.
    /// </summary>
    internal static double NormalCdf(double x, double mean, double stdDev)
    {
        double z = (x - mean) / (stdDev * Math.Sqrt(2.0));
        return 0.5 * (1.0 + Erf(z));
    }

    /// <summary>
    /// Error function approximation — Abramowitz &amp; Stegun formula 7.1.26.
    /// Maximum error ≤ 1.5×10⁻⁷.
    /// </summary>
    internal static double Erf(double x)
    {
        bool negative = x < 0;
        if (negative) x = -x;

        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        double t = 1.0 / (1.0 + p * x);
        double t2 = t * t;
        double t3 = t2 * t;
        double t4 = t3 * t;
        double t5 = t4 * t;
        double y = 1.0 - (a1 * t + a2 * t2 + a3 * t3 + a4 * t4 + a5 * t5) * Math.Exp(-x * x);

        return negative ? -y : y;
    }

    // ────────────────────────────────────────────────────────────
    //  Binomial Distribution
    // ────────────────────────────────────────────────────────────

    public BinomialDistributionResult Binomial(int n, double p, int? k = null)
    {
        _logger.LogDebug("Computing Binomial distribution: n={N}, p={P}, k={K}", n, p, k);
        if (n < 0 || n > 1000)
            return BinomialDistributionResult.Fail("n must be between 0 and 1,000.");
        if (p < 0 || p > 1)
            return BinomialDistributionResult.Fail("p must be between 0 and 1.");
        if (k.HasValue && (k.Value < 0 || k.Value > n))
            return BinomialDistributionResult.Fail($"k must be between 0 and n ({n}).");

        double expectedValue = n * p;
        double variance = n * p * (1 - p);
        double stdDev = Math.Sqrt(variance);

        double? pmfAtK = k.HasValue ? BinomialPmf(k.Value, n, p) : null;
        double? cdfAtK = null;
        if (k.HasValue)
        {
            double cumulative = 0;
            for (int i = 0; i <= k.Value; i++)
                cumulative += BinomialPmf(i, n, p);
            cdfAtK = Math.Min(cumulative, 1.0);
        }

        // PMF points for the full range (or clipped to meaningful range)
        int plotMax = n;
        // For large n, only plot the region with meaningful probability
        if (n > 100)
        {
            plotMax = Math.Min(n, (int)Math.Ceiling(expectedValue + 5 * stdDev));
        }
        int plotMin = 0;
        if (n > 100)
        {
            plotMin = Math.Max(0, (int)Math.Floor(expectedValue - 5 * stdDev));
        }

        var points = new List<DistributionPoint>();
        for (int i = plotMin; i <= plotMax; i++)
        {
            double pmf = BinomialPmf(i, n, p);
            points.Add(new DistributionPoint { X = i, Y = Math.Round(pmf, 8) });
        }

        return new BinomialDistributionResult
        {
            Success = true,
            N = n,
            P = p,
            ExpectedValue = Math.Round(expectedValue, 6),
            Variance = Math.Round(variance, 6),
            StdDev = Math.Round(stdDev, 6),
            PmfAtK = pmfAtK.HasValue ? Math.Round(pmfAtK.Value, 8) : null,
            CdfAtK = cdfAtK.HasValue ? Math.Round(cdfAtK.Value, 8) : null,
            PmfPoints = [.. points],
        };
    }

    internal static double BinomialPmf(int k, int n, double p)
    {
        // Use log-space to avoid overflow for large n
        double logPmf = LogBinomialCoefficient(n, k) + k * Math.Log(p) + (n - k) * Math.Log(1 - p);

        // Handle edge cases where p=0 or p=1
        if (p == 0) return k == 0 ? 1.0 : 0.0;
        if (p == 1) return k == n ? 1.0 : 0.0;

        return Math.Exp(logPmf);
    }

    internal static double LogBinomialCoefficient(int n, int k)
    {
        if (k == 0 || k == n) return 0;
        if (k == 1 || k == n - 1) return Math.Log(n);
        // Use LogGamma for efficiency
        return LogFactorial(n) - LogFactorial(k) - LogFactorial(n - k);
    }

    internal static double LogFactorial(int n)
    {
        if (n <= 1) return 0;
        // Use Stirling's approximation for large n, exact for small n
        double sum = 0;
        for (int i = 2; i <= n; i++)
            sum += Math.Log(i);
        return sum;
    }

    // ────────────────────────────────────────────────────────────
    //  Poisson Distribution
    // ────────────────────────────────────────────────────────────

    public PoissonDistributionResult Poisson(double lambda, int? k = null)
    {
        _logger.LogDebug("Computing Poisson distribution: lambda={Lambda}, k={K}", lambda, k);
        if (lambda <= 0 || lambda > 1000)
            return PoissonDistributionResult.Fail("λ must be between 0 (exclusive) and 1,000.");
        if (double.IsNaN(lambda) || double.IsInfinity(lambda))
            return PoissonDistributionResult.Fail("λ must be a finite positive number.");
        if (k.HasValue && k.Value < 0)
            return PoissonDistributionResult.Fail("k must be non-negative.");

        double stdDev = Math.Sqrt(lambda);

        double? pmfAtK = k.HasValue ? PoissonPmf(k.Value, lambda) : null;
        double? cdfAtK = null;
        if (k.HasValue)
        {
            double cumulative = 0;
            for (int i = 0; i <= k.Value; i++)
                cumulative += PoissonPmf(i, lambda);
            cdfAtK = Math.Min(cumulative, 1.0);
        }

        // Plot range: 0 to mean + 5σ (or at least 20)
        int plotMax = Math.Max(20, (int)Math.Ceiling(lambda + 5 * stdDev));
        plotMax = Math.Min(plotMax, 200); // cap for rendering

        var points = new List<DistributionPoint>();
        for (int i = 0; i <= plotMax; i++)
        {
            double pmf = PoissonPmf(i, lambda);
            if (i > lambda + 4 * stdDev && pmf < 1e-10) break; // stop when negligible
            points.Add(new DistributionPoint { X = i, Y = Math.Round(pmf, 8) });
        }

        return new PoissonDistributionResult
        {
            Success = true,
            Lambda = lambda,
            ExpectedValue = Math.Round(lambda, 6),
            Variance = Math.Round(lambda, 6),
            StdDev = Math.Round(stdDev, 6),
            PmfAtK = pmfAtK.HasValue ? Math.Round(pmfAtK.Value, 8) : null,
            CdfAtK = cdfAtK.HasValue ? Math.Round(cdfAtK.Value, 8) : null,
            PmfPoints = [.. points],
        };
    }

    internal static double PoissonPmf(int k, double lambda)
    {
        if (k < 0) return 0;
        // Use log-space: PMF = e^(-λ) * λ^k / k!
        double logPmf = -lambda + k * Math.Log(lambda) - LogFactorial(k);
        return Math.Exp(logPmf);
    }
}
