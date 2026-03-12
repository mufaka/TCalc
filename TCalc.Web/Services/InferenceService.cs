namespace TCalc.Web.Services;

/// <summary>
/// Implements statistical inference: Bayesian updating,
/// hypothesis testing, maximum likelihood estimation, and confidence intervals.
/// </summary>
public sealed class InferenceService : IInferenceService
{
    private readonly ILogger<InferenceService> _logger;

    public InferenceService(ILogger<InferenceService> logger) => _logger = logger;

    // ════════════════════════════════════════════════════════════════
    //  Bayesian Update (Beta-Binomial conjugate model)
    // ════════════════════════════════════════════════════════════════

    public BayesianUpdateResult BayesianUpdate(double priorAlpha, double priorBeta, int successes, int trials)
    {
        if (priorAlpha <= 0)
            return BayesianUpdateResult.Fail("Prior α must be positive.");
        if (priorBeta <= 0)
            return BayesianUpdateResult.Fail("Prior β must be positive.");
        if (successes < 0)
            return BayesianUpdateResult.Fail("Number of successes must be non-negative.");
        if (trials < 0)
            return BayesianUpdateResult.Fail("Number of trials must be non-negative.");
        if (successes > trials)
            return BayesianUpdateResult.Fail("Successes cannot exceed trials.");

        _logger.LogDebug("BayesianUpdate: α={Alpha}, β={Beta}, k={K}, n={N}",
            priorAlpha, priorBeta, successes, trials);

        double postAlpha = priorAlpha + successes;
        double postBeta = priorBeta + (trials - successes);
        double postMean = postAlpha / (postAlpha + postBeta);

        // 95% credible interval via Beta quantile approximation
        double credLower = BetaQuantile(0.025, postAlpha, postBeta);
        double credUpper = BetaQuantile(0.975, postAlpha, postBeta);

        // Generate curves over θ ∈ [0, 1]
        const int points = 200;
        var priorCurve = new DistributionPoint[points];
        var likeCurve = new DistributionPoint[points];
        var postCurve = new DistributionPoint[points];

        // Find max likelihood for normalization (so it renders on the same scale)
        double maxLike = 0;
        for (int i = 0; i < points; i++)
        {
            double theta = (i + 0.5) / points; // avoid exact 0 and 1
            double like = BinomialLikelihood(theta, successes, trials);
            if (like > maxLike) maxLike = like;
        }
        if (maxLike == 0) maxLike = 1;

        // Find max posterior for likelihood scaling target
        double maxPost = 0;
        for (int i = 0; i < points; i++)
        {
            double theta = (i + 0.5) / points;
            double post = BetaPdf(theta, postAlpha, postBeta);
            if (post > maxPost) maxPost = post;
        }
        double likeScale = maxPost > 0 ? maxPost / maxLike : 1;

        for (int i = 0; i < points; i++)
        {
            double theta = (i + 0.5) / points;
            priorCurve[i] = new DistributionPoint
            {
                X = Math.Round(theta, 6),
                Y = Math.Round(BetaPdf(theta, priorAlpha, priorBeta), 8),
            };
            likeCurve[i] = new DistributionPoint
            {
                X = Math.Round(theta, 6),
                Y = Math.Round(BinomialLikelihood(theta, successes, trials) * likeScale, 8),
            };
            postCurve[i] = new DistributionPoint
            {
                X = Math.Round(theta, 6),
                Y = Math.Round(BetaPdf(theta, postAlpha, postBeta), 8),
            };
        }

        return new BayesianUpdateResult
        {
            Success = true,
            PriorAlpha = priorAlpha,
            PriorBeta = priorBeta,
            PosteriorAlpha = Math.Round(postAlpha, 6),
            PosteriorBeta = Math.Round(postBeta, 6),
            PosteriorMean = Math.Round(postMean, 6),
            CredibleLower = Math.Round(credLower, 6),
            CredibleUpper = Math.Round(credUpper, 6),
            PriorCurve = priorCurve,
            LikelihoodCurve = likeCurve,
            PosteriorCurve = postCurve,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  Z-Test (one-sample, known σ)
    // ════════════════════════════════════════════════════════════════

    public ZTestResult ZTest(double sampleMean, double populationMean, double populationStdDev,
                             int sampleSize, double alpha, string alternative)
    {
        if (populationStdDev <= 0)
            return ZTestResult.Fail("Population standard deviation must be positive.");
        if (sampleSize < 1)
            return ZTestResult.Fail("Sample size must be at least 1.");
        if (alpha <= 0 || alpha >= 1)
            return ZTestResult.Fail("Significance level α must be between 0 and 1.");

        alternative = (alternative ?? "two-tailed").Trim().ToLowerInvariant();
        if (alternative is not ("two-tailed" or "left" or "right"))
            return ZTestResult.Fail("Alternative must be 'two-tailed', 'left', or 'right'.");

        _logger.LogDebug("ZTest: x̄={XBar}, μ₀={Mu0}, σ={Sigma}, n={N}, α={Alpha}, alt={Alt}",
            sampleMean, populationMean, populationStdDev, sampleSize, alpha, alternative);

        double se = populationStdDev / Math.Sqrt(sampleSize);
        double z = (sampleMean - populationMean) / se;

        double pValue;
        double criticalValue;
        switch (alternative)
        {
            case "left":
                pValue = StandardNormalCdf(z);
                criticalValue = StandardNormalQuantile(alpha);
                break;
            case "right":
                pValue = 1.0 - StandardNormalCdf(z);
                criticalValue = StandardNormalQuantile(1.0 - alpha);
                break;
            default: // two-tailed
                pValue = 2.0 * (1.0 - StandardNormalCdf(Math.Abs(z)));
                criticalValue = StandardNormalQuantile(1.0 - alpha / 2.0);
                break;
        }

        bool reject = pValue < alpha;

        // Generate standard normal curve for visualization
        var curve = GenerateStandardNormalCurve();

        return new ZTestResult
        {
            Success = true,
            TestStatistic = Math.Round(z, 6),
            PValue = Math.Round(pValue, 8),
            CriticalValue = Math.Round(criticalValue, 6),
            RejectNull = reject,
            Alternative = alternative,
            Alpha = alpha,
            DistributionCurve = curve,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  T-Test (one-sample, unknown σ)
    // ════════════════════════════════════════════════════════════════

    public TTestResult TTest(double sampleMean, double populationMean, double sampleStdDev,
                             int sampleSize, double alpha, string alternative)
    {
        if (sampleStdDev <= 0)
            return TTestResult.Fail("Sample standard deviation must be positive.");
        if (sampleSize < 2)
            return TTestResult.Fail("Sample size must be at least 2.");
        if (alpha <= 0 || alpha >= 1)
            return TTestResult.Fail("Significance level α must be between 0 and 1.");

        alternative = (alternative ?? "two-tailed").Trim().ToLowerInvariant();
        if (alternative is not ("two-tailed" or "left" or "right"))
            return TTestResult.Fail("Alternative must be 'two-tailed', 'left', or 'right'.");

        int df = sampleSize - 1;

        _logger.LogDebug("TTest: x̄={XBar}, μ₀={Mu0}, s={S}, n={N}, df={DF}, α={Alpha}, alt={Alt}",
            sampleMean, populationMean, sampleStdDev, sampleSize, df, alpha, alternative);

        double se = sampleStdDev / Math.Sqrt(sampleSize);
        double t = (sampleMean - populationMean) / se;

        double pValue;
        double criticalValue;
        switch (alternative)
        {
            case "left":
                pValue = TDistCdf(t, df);
                criticalValue = TDistQuantile(alpha, df);
                break;
            case "right":
                pValue = 1.0 - TDistCdf(t, df);
                criticalValue = TDistQuantile(1.0 - alpha, df);
                break;
            default: // two-tailed
                pValue = 2.0 * (1.0 - TDistCdf(Math.Abs(t), df));
                criticalValue = TDistQuantile(1.0 - alpha / 2.0, df);
                break;
        }

        bool reject = pValue < alpha;

        // Generate t-distribution curve for visualization
        var curve = GenerateTDistCurve(df);

        return new TTestResult
        {
            Success = true,
            TestStatistic = Math.Round(t, 6),
            PValue = Math.Round(pValue, 8),
            CriticalValue = Math.Round(criticalValue, 6),
            RejectNull = reject,
            Alternative = alternative,
            Alpha = alpha,
            DegreesOfFreedom = df,
            DistributionCurve = curve,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  Chi-Square Goodness-of-Fit Test
    // ════════════════════════════════════════════════════════════════

    public ChiSquareTestResult ChiSquareTest(double[] observed, double[] expected, double alpha)
    {
        if (observed is null || observed.Length < 2)
            return ChiSquareTestResult.Fail("At least 2 observed categories are required.");
        if (expected is null || expected.Length != observed.Length)
            return ChiSquareTestResult.Fail("Expected array must have the same length as observed.");
        if (alpha <= 0 || alpha >= 1)
            return ChiSquareTestResult.Fail("Significance level α must be between 0 and 1.");

        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] <= 0)
                return ChiSquareTestResult.Fail($"Expected value at index {i} must be positive.");
        }

        int df = observed.Length - 1;

        _logger.LogDebug("ChiSquareTest: {K} categories, df={DF}, α={Alpha}", observed.Length, df, alpha);

        double chiSq = 0;
        for (int i = 0; i < observed.Length; i++)
        {
            double diff = observed[i] - expected[i];
            chiSq += (diff * diff) / expected[i];
        }

        double pValue = 1.0 - ChiSquareCdf(chiSq, df);
        double criticalValue = ChiSquareQuantile(1.0 - alpha, df);
        bool reject = chiSq > criticalValue;

        return new ChiSquareTestResult
        {
            Success = true,
            TestStatistic = Math.Round(chiSq, 6),
            PValue = Math.Round(pValue, 8),
            CriticalValue = Math.Round(criticalValue, 6),
            RejectNull = reject,
            Alpha = alpha,
            DegreesOfFreedom = df,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  MLE — Normal Distribution
    // ════════════════════════════════════════════════════════════════

    public MleNormalResult MleNormal(double[] data)
    {
        if (data is null || data.Length < 2)
            return MleNormalResult.Fail("At least 2 data points are required.");
        if (data.Length > 100_000)
            return MleNormalResult.Fail("Data set must not exceed 100,000 values.");

        _logger.LogDebug("MleNormal: {N} data points", data.Length);

        int n = data.Length;

        // MLE estimates
        double muHat = 0;
        for (int i = 0; i < n; i++) muHat += data[i];
        muHat /= n;

        double sigmaHatSq = 0;
        for (int i = 0; i < n; i++)
        {
            double diff = data[i] - muHat;
            sigmaHatSq += diff * diff;
        }
        sigmaHatSq /= n; // MLE uses n, not n-1
        double sigmaHat = Math.Sqrt(sigmaHatSq);

        if (sigmaHat < 1e-15)
            return MleNormalResult.Fail("All data values are identical; σ cannot be estimated.");

        double logLik = NormalLogLikelihood(data, muHat, sigmaHat);

        // Generate contour data: grid of (μ, σ) values around the MLE
        const int gridSize = 30;
        double muRange = Math.Max(sigmaHat * 2, 1);
        double sigmaLo = Math.Max(sigmaHat * 0.3, 1e-6);
        double sigmaHi = sigmaHat * 2.0;

        double muStep = (2 * muRange) / (gridSize - 1);
        double sigmaStep = (sigmaHi - sigmaLo) / (gridSize - 1);

        var muAxis = new double[gridSize];
        var sigmaAxis = new double[gridSize];
        var contour = new List<MleContourPoint>(gridSize * gridSize);

        for (int i = 0; i < gridSize; i++)
        {
            muAxis[i] = Math.Round(muHat - muRange + i * muStep, 6);
            sigmaAxis[i] = Math.Round(sigmaLo + i * sigmaStep, 6);
        }

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                double ll = NormalLogLikelihood(data, muAxis[i], sigmaAxis[j]);
                contour.Add(new MleContourPoint
                {
                    Mu = muAxis[i],
                    Sigma = sigmaAxis[j],
                    LogLikelihood = Math.Round(ll, 4),
                });
            }
        }

        return new MleNormalResult
        {
            Success = true,
            EstimatedMean = Math.Round(muHat, 6),
            EstimatedStdDev = Math.Round(sigmaHat, 6),
            LogLikelihood = Math.Round(logLik, 6),
            ContourData = [.. contour],
            MuAxis = muAxis,
            SigmaAxis = sigmaAxis,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  MLE — Binomial Distribution
    // ════════════════════════════════════════════════════════════════

    public MleBinomialResult MleBinomial(int trials, int successes)
    {
        if (trials < 1)
            return MleBinomialResult.Fail("Number of trials must be at least 1.");
        if (successes < 0)
            return MleBinomialResult.Fail("Number of successes must be non-negative.");
        if (successes > trials)
            return MleBinomialResult.Fail("Successes cannot exceed trials.");

        _logger.LogDebug("MleBinomial: n={N}, k={K}", trials, successes);

        double pHat = (double)successes / trials;
        double logLik = BinomialLogLikelihood(pHat, successes, trials);

        // Log-likelihood curve over p ∈ (0, 1)
        const int points = 200;
        var curve = new DistributionPoint[points];
        for (int i = 0; i < points; i++)
        {
            double p = (i + 0.5) / points; // avoid exact 0 and 1
            double ll = BinomialLogLikelihood(p, successes, trials);
            curve[i] = new DistributionPoint
            {
                X = Math.Round(p, 6),
                Y = Math.Round(ll, 6),
            };
        }

        return new MleBinomialResult
        {
            Success = true,
            Trials = trials,
            Successes = successes,
            EstimatedP = Math.Round(pHat, 8),
            LogLikelihood = Math.Round(logLik, 6),
            LogLikelihoodCurve = curve,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  Confidence Interval
    // ════════════════════════════════════════════════════════════════

    public ConfidenceIntervalResult ConfidenceInterval(double sampleMean, double stdDev,
        int sampleSize, double confidenceLevel, bool isPopulationStdDev)
    {
        if (stdDev <= 0)
            return ConfidenceIntervalResult.Fail("Standard deviation must be positive.");
        if (sampleSize < 1)
            return ConfidenceIntervalResult.Fail("Sample size must be at least 1.");
        if (confidenceLevel <= 0 || confidenceLevel >= 1)
            return ConfidenceIntervalResult.Fail("Confidence level must be between 0 and 1 (e.g. 0.95).");

        _logger.LogDebug("ConfidenceInterval: x̄={XBar}, σ/s={Sd}, n={N}, CL={CL}, popσ={PopSd}",
            sampleMean, stdDev, sampleSize, confidenceLevel, isPopulationStdDev);

        double tailArea = (1.0 - confidenceLevel) / 2.0;
        double criticalValue;
        string intervalType;

        if (isPopulationStdDev || sampleSize >= 30)
        {
            // Z-interval
            criticalValue = StandardNormalQuantile(1.0 - tailArea);
            intervalType = "z";
        }
        else
        {
            // T-interval
            int df = sampleSize - 1;
            if (df < 1)
                return ConfidenceIntervalResult.Fail("Sample size must be at least 2 for a t-interval.");
            criticalValue = TDistQuantile(1.0 - tailArea, df);
            intervalType = "t";
        }

        double se = stdDev / Math.Sqrt(sampleSize);
        double margin = criticalValue * se;

        return new ConfidenceIntervalResult
        {
            Success = true,
            SampleMean = sampleMean,
            StdDev = stdDev,
            SampleSize = sampleSize,
            ConfidenceLevel = confidenceLevel,
            MarginOfError = Math.Round(margin, 6),
            LowerBound = Math.Round(sampleMean - margin, 6),
            UpperBound = Math.Round(sampleMean + margin, 6),
            CriticalValue = Math.Round(criticalValue, 6),
            IntervalType = intervalType,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  Math Helpers — Beta Distribution
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Beta PDF: f(x; α, β) = x^(α-1) (1-x)^(β-1) / B(α, β)
    /// </summary>
    internal static double BetaPdf(double x, double alpha, double beta)
    {
        if (x <= 0 || x >= 1) return 0;
        double logPdf = (alpha - 1) * Math.Log(x) + (beta - 1) * Math.Log(1 - x) - LogBeta(alpha, beta);
        return Math.Exp(logPdf);
    }

    /// <summary>
    /// Log of the Beta function: log B(α, β) = log Γ(α) + log Γ(β) − log Γ(α + β)
    /// </summary>
    internal static double LogBeta(double a, double b) =>
        LogGamma(a) + LogGamma(b) - LogGamma(a + b);

    /// <summary>
    /// Lanczos approximation of the log-Gamma function.
    /// </summary>
    internal static double LogGamma(double x)
    {
        if (x <= 0) return double.PositiveInfinity;

        double[] coef =
        [
            76.18009172947146,
            -86.50532032941677,
            24.01409824083091,
            -1.231739572450155,
            0.001208650973866179,
            -0.000005395239384953,
        ];

        double y = x;
        double tmp = x + 5.5;
        tmp -= (x + 0.5) * Math.Log(tmp);

        double ser = 1.000000000190015;
        for (int j = 0; j < coef.Length; j++)
        {
            y += 1;
            ser += coef[j] / y;
        }

        return -tmp + Math.Log(2.5066282746310005 * ser / x);
    }

    /// <summary>
    /// Approximate Beta quantile using Newton's method on the regularized incomplete beta function.
    /// Falls back to a bisection search for robustness.
    /// </summary>
    internal static double BetaQuantile(double p, double alpha, double beta)
    {
        if (p <= 0) return 0;
        if (p >= 1) return 1;

        // Bisection on the regularized incomplete beta function
        double lo = 0, hi = 1;
        for (int i = 0; i < 100; i++)
        {
            double mid = (lo + hi) / 2.0;
            double cdf = RegularizedIncompleteBeta(mid, alpha, beta);
            if (cdf < p) lo = mid;
            else hi = mid;
            if (hi - lo < 1e-10) break;
        }
        return (lo + hi) / 2.0;
    }

    /// <summary>
    /// Regularized incomplete beta function I_x(a, b) via continued fraction (Lentz's method).
    /// </summary>
    internal static double RegularizedIncompleteBeta(double x, double a, double b)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;

        // For numerical stability, use the symmetry relation when appropriate
        if (x > (a + 1) / (a + b + 2))
            return 1.0 - RegularizedIncompleteBeta(1.0 - x, b, a);

        double logPrefix = a * Math.Log(x) + b * Math.Log(1.0 - x) - Math.Log(a) - LogBeta(a, b);
        double prefix = Math.Exp(logPrefix);

        // Continued fraction using the modified Lentz method
        const double tiny = 1e-30;
        const int maxIter = 200;
        const double eps = 1e-12;

        double c = 1.0;
        double d = 1.0 - (a + b) * x / (a + 1.0);
        if (Math.Abs(d) < tiny) d = tiny;
        d = 1.0 / d;
        double h = d;

        for (int m = 1; m <= maxIter; m++)
        {
            // Even step
            double num = m * (b - m) * x / ((a + 2.0 * m - 1.0) * (a + 2.0 * m));
            d = 1.0 + num * d;
            if (Math.Abs(d) < tiny) d = tiny;
            c = 1.0 + num / c;
            if (Math.Abs(c) < tiny) c = tiny;
            d = 1.0 / d;
            h *= d * c;

            // Odd step
            num = -(a + m) * (a + b + m) * x / ((a + 2.0 * m) * (a + 2.0 * m + 1.0));
            d = 1.0 + num * d;
            if (Math.Abs(d) < tiny) d = tiny;
            c = 1.0 + num / c;
            if (Math.Abs(c) < tiny) c = tiny;
            d = 1.0 / d;
            double delta = d * c;
            h *= delta;

            if (Math.Abs(delta - 1.0) < eps) break;
        }

        return prefix * h;
    }

    // ════════════════════════════════════════════════════════════════
    //  Math Helpers — Binomial Likelihood
    // ════════════════════════════════════════════════════════════════

    internal static double BinomialLikelihood(double p, int k, int n)
    {
        if (p <= 0 || p >= 1)
        {
            if (p <= 0) return k == 0 ? 1.0 : 0.0;
            return k == n ? 1.0 : 0.0;
        }
        double logLik = k * Math.Log(p) + (n - k) * Math.Log(1 - p);
        return Math.Exp(logLik);
    }

    internal static double BinomialLogLikelihood(double p, int k, int n)
    {
        if (p <= 0 || p >= 1)
        {
            if (p <= 0) return k == 0 ? 0 : double.NegativeInfinity;
            return k == n ? 0 : double.NegativeInfinity;
        }
        return k * Math.Log(p) + (n - k) * Math.Log(1 - p);
    }

    // ════════════════════════════════════════════════════════════════
    //  Math Helpers — Normal Log-Likelihood
    // ════════════════════════════════════════════════════════════════

    internal static double NormalLogLikelihood(double[] data, double mu, double sigma)
    {
        if (sigma <= 0) return double.NegativeInfinity;
        int n = data.Length;
        double logLik = -n * Math.Log(sigma) - n * 0.5 * Math.Log(2 * Math.PI);
        double sumSq = 0;
        for (int i = 0; i < n; i++)
        {
            double diff = data[i] - mu;
            sumSq += diff * diff;
        }
        logLik -= sumSq / (2 * sigma * sigma);
        return logLik;
    }

    // ════════════════════════════════════════════════════════════════
    //  Math Helpers — Standard Normal CDF / Quantile
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Standard normal CDF Φ(z) using the error function.
    /// </summary>
    internal static double StandardNormalCdf(double z) =>
        0.5 * (1.0 + ProbabilityService.Erf(z / Math.Sqrt(2.0)));

    /// <summary>
    /// Standard normal quantile (inverse CDF) using the rational approximation
    /// by Peter Acklam.
    /// </summary>
    internal static double StandardNormalQuantile(double p)
    {
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;
        if (Math.Abs(p - 0.5) < 1e-15) return 0;

        // Coefficients for the rational approximation
        const double a1 = -3.969683028665376e+01;
        const double a2 = 2.209460984245205e+02;
        const double a3 = -2.759285104469687e+02;
        const double a4 = 1.383577518672690e+02;
        const double a5 = -3.066479806614716e+01;
        const double a6 = 2.506628277459239e+00;

        const double b1 = -5.447609879822406e+01;
        const double b2 = 1.615858368580409e+02;
        const double b3 = -1.556989798598866e+02;
        const double b4 = 6.680131188771972e+01;
        const double b5 = -1.328068155288572e+01;

        const double c1 = -7.784894002430293e-03;
        const double c2 = -3.223964580411365e-01;
        const double c3 = -2.400758277161838e+00;
        const double c4 = -2.549732539343734e+00;
        const double c5 = 4.374664141464968e+00;
        const double c6 = 2.938163982698783e+00;

        const double d1 = 7.784695709041462e-03;
        const double d2 = 3.224671290700398e-01;
        const double d3 = 2.445134137142996e+00;
        const double d4 = 3.754408661907416e+00;

        const double pLow = 0.02425;
        const double pHigh = 1 - pLow;

        double q, r;

        if (p < pLow)
        {
            // Rational approximation for lower region
            q = Math.Sqrt(-2 * Math.Log(p));
            return (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                   ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
        else if (p <= pHigh)
        {
            // Rational approximation for central region
            q = p - 0.5;
            r = q * q;
            return (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q /
                   (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
        }
        else
        {
            // Rational approximation for upper region
            q = Math.Sqrt(-2 * Math.Log(1 - p));
            return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                    ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Math Helpers — T-Distribution CDF / Quantile
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-distribution PDF: f(t; ν) = Γ((ν+1)/2) / (√(νπ) Γ(ν/2)) · (1 + t²/ν)^(-(ν+1)/2)
    /// </summary>
    internal static double TDistPdf(double t, int df)
    {
        double logPdf = LogGamma((df + 1.0) / 2.0) - LogGamma(df / 2.0)
                       - 0.5 * Math.Log(df * Math.PI)
                       - ((df + 1.0) / 2.0) * Math.Log(1.0 + t * t / df);
        return Math.Exp(logPdf);
    }

    /// <summary>
    /// T-distribution CDF via the regularized incomplete beta function.
    /// </summary>
    internal static double TDistCdf(double t, int df)
    {
        double x = df / (df + t * t);
        double ibeta = RegularizedIncompleteBeta(x, df / 2.0, 0.5);
        double cdf = 1.0 - 0.5 * ibeta;
        return t >= 0 ? cdf : 1.0 - cdf;
    }

    /// <summary>
    /// T-distribution quantile via bisection on TDistCdf.
    /// </summary>
    internal static double TDistQuantile(double p, int df)
    {
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;
        if (Math.Abs(p - 0.5) < 1e-15) return 0;

        // Bisection search
        double lo = -100, hi = 100;
        for (int i = 0; i < 100; i++)
        {
            double mid = (lo + hi) / 2.0;
            double cdf = TDistCdf(mid, df);
            if (cdf < p) lo = mid;
            else hi = mid;
            if (hi - lo < 1e-10) break;
        }
        return (lo + hi) / 2.0;
    }

    // ════════════════════════════════════════════════════════════════
    //  Math Helpers — Chi-Square Distribution CDF / Quantile
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chi-square CDF via the regularized lower incomplete gamma function.
    /// χ² CDF(x, k) = γ(k/2, x/2) / Γ(k/2) = P(k/2, x/2)
    /// </summary>
    internal static double ChiSquareCdf(double x, int df)
    {
        if (x <= 0) return 0;
        return RegularizedLowerIncompleteGamma(df / 2.0, x / 2.0);
    }

    /// <summary>
    /// Chi-square quantile via bisection.
    /// </summary>
    internal static double ChiSquareQuantile(double p, int df)
    {
        if (p <= 0) return 0;
        if (p >= 1) return double.PositiveInfinity;

        double lo = 0, hi = Math.Max(df * 5.0, 100);
        // Extend hi if needed
        while (ChiSquareCdf(hi, df) < p) hi *= 2;

        for (int i = 0; i < 100; i++)
        {
            double mid = (lo + hi) / 2.0;
            double cdf = ChiSquareCdf(mid, df);
            if (cdf < p) lo = mid;
            else hi = mid;
            if (hi - lo < 1e-10) break;
        }
        return (lo + hi) / 2.0;
    }

    /// <summary>
    /// Regularized lower incomplete gamma function P(a, x) = γ(a, x) / Γ(a)
    /// using the series expansion.
    /// </summary>
    internal static double RegularizedLowerIncompleteGamma(double a, double x)
    {
        if (x <= 0) return 0;
        if (x > a + 1)
        {
            // Use the complement: P(a,x) = 1 - Q(a,x) where Q uses continued fraction
            return 1.0 - RegularizedUpperIncompleteGamma(a, x);
        }

        // Series expansion: P(a,x) = e^(-x) x^a Σ_{n=0}^∞ x^n / Γ(a+n+1)
        double sum = 1.0 / a;
        double term = 1.0 / a;
        for (int n = 1; n < 200; n++)
        {
            term *= x / (a + n);
            sum += term;
            if (Math.Abs(term) < Math.Abs(sum) * 1e-12) break;
        }

        double logPrefix = a * Math.Log(x) - x - LogGamma(a);
        return sum * Math.Exp(logPrefix);
    }

    /// <summary>
    /// Regularized upper incomplete gamma function Q(a, x) = Γ(a, x) / Γ(a)
    /// using the continued fraction representation.
    /// </summary>
    internal static double RegularizedUpperIncompleteGamma(double a, double x)
    {
        if (x <= 0) return 1;

        const double tiny = 1e-30;
        double b0 = x + 1.0 - a;
        double c = 1.0 / tiny;
        double d = 1.0 / b0;
        double h = d;

        for (int i = 1; i <= 200; i++)
        {
            double an = -i * (i - a);
            double bn = x + 2.0 * i + 1.0 - a;
            d = an * d + bn;
            if (Math.Abs(d) < tiny) d = tiny;
            c = bn + an / c;
            if (Math.Abs(c) < tiny) c = tiny;
            d = 1.0 / d;
            double delta = d * c;
            h *= delta;
            if (Math.Abs(delta - 1.0) < 1e-12) break;
        }

        double logPrefix = a * Math.Log(x) - x - LogGamma(a);
        return Math.Exp(logPrefix) * h;
    }

    // ════════════════════════════════════════════════════════════════
    //  Chart Curve Generation
    // ════════════════════════════════════════════════════════════════

    private static DistributionPoint[] GenerateStandardNormalCurve()
    {
        const int n = 200;
        var curve = new DistributionPoint[n];
        double lo = -4, hi = 4;
        double step = (hi - lo) / (n - 1);
        for (int i = 0; i < n; i++)
        {
            double x = lo + i * step;
            curve[i] = new DistributionPoint
            {
                X = Math.Round(x, 6),
                Y = Math.Round(ProbabilityService.NormalPdf(x, 0, 1), 8),
            };
        }
        return curve;
    }

    private static DistributionPoint[] GenerateTDistCurve(int df)
    {
        const int n = 200;
        var curve = new DistributionPoint[n];
        double lo = -4, hi = 4;
        double step = (hi - lo) / (n - 1);
        for (int i = 0; i < n; i++)
        {
            double x = lo + i * step;
            curve[i] = new DistributionPoint
            {
                X = Math.Round(x, 6),
                Y = Math.Round(TDistPdf(x, df), 8),
            };
        }
        return curve;
    }
}
