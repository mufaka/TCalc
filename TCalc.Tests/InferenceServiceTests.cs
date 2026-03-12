using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class InferenceServiceTests
{
    private readonly InferenceService _service = new(NullLogger<InferenceService>.Instance);

    // ════════════════════════════════════════════════════════════════
    //  Bayesian Update
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void BayesianUpdate_UniformPrior_PosteriorMatchesFormula()
    {
        // Uniform prior Beta(1,1), observe 7 successes in 10 trials
        var result = _service.BayesianUpdate(1, 1, 7, 10);

        Assert.True(result.Success);
        Assert.Equal(8, result.PosteriorAlpha);   // 1 + 7
        Assert.Equal(4, result.PosteriorBeta);     // 1 + (10 - 7)
        Assert.InRange(result.PosteriorMean, 0.66, 0.67); // 8/12 ≈ 0.6667
    }

    [Fact]
    public void BayesianUpdate_InformativePrior_PosteriorUpdatesCorrectly()
    {
        // Beta(5, 5) prior, observe 3 successes in 5 trials
        var result = _service.BayesianUpdate(5, 5, 3, 5);

        Assert.True(result.Success);
        Assert.Equal(8, result.PosteriorAlpha);    // 5 + 3
        Assert.Equal(7, result.PosteriorBeta);     // 5 + 2
        Assert.InRange(result.PosteriorMean, 0.53, 0.54); // 8/15 ≈ 0.5333
    }

    [Fact]
    public void BayesianUpdate_ZeroEvidence_PosteriorEqualsPrior()
    {
        var result = _service.BayesianUpdate(3, 7, 0, 0);

        Assert.True(result.Success);
        Assert.Equal(3, result.PosteriorAlpha);
        Assert.Equal(7, result.PosteriorBeta);
    }

    [Fact]
    public void BayesianUpdate_GeneratesCurves()
    {
        var result = _service.BayesianUpdate(1, 1, 5, 10);

        Assert.True(result.Success);
        Assert.Equal(200, result.PriorCurve.Length);
        Assert.Equal(200, result.LikelihoodCurve.Length);
        Assert.Equal(200, result.PosteriorCurve.Length);

        // All X values should be in (0, 1)
        foreach (var pt in result.PriorCurve)
            Assert.InRange(pt.X, 0, 1);
        foreach (var pt in result.PosteriorCurve)
            Assert.InRange(pt.X, 0, 1);
    }

    [Fact]
    public void BayesianUpdate_CredibleIntervalContainsPosteriorMean()
    {
        var result = _service.BayesianUpdate(2, 2, 8, 10);

        Assert.True(result.Success);
        Assert.True(result.CredibleLower < result.PosteriorMean);
        Assert.True(result.CredibleUpper > result.PosteriorMean);
        Assert.True(result.CredibleLower >= 0);
        Assert.True(result.CredibleUpper <= 1);
    }

    [Fact]
    public void BayesianUpdate_InvalidInputs_ReturnErrors()
    {
        Assert.False(_service.BayesianUpdate(0, 1, 0, 0).Success);    // α = 0
        Assert.False(_service.BayesianUpdate(1, -1, 0, 0).Success);   // β < 0
        Assert.False(_service.BayesianUpdate(1, 1, -1, 0).Success);   // successes < 0
        Assert.False(_service.BayesianUpdate(1, 1, 5, 3).Success);    // successes > trials
    }

    // ════════════════════════════════════════════════════════════════
    //  Z-Test
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ZTest_KnownExample_TwoTailed()
    {
        // Population μ₀=100, σ=15, sample mean=105, n=36
        // z = (105-100)/(15/6) = 5/2.5 = 2.0
        var result = _service.ZTest(105, 100, 15, 36, 0.05, "two-tailed");

        Assert.True(result.Success);
        Assert.InRange(result.TestStatistic, 1.99, 2.01);
        Assert.InRange(result.PValue, 0.04, 0.06); // ~0.0455
        Assert.True(result.RejectNull); // 0.0455 < 0.05
    }

    [Fact]
    public void ZTest_LeftTailed()
    {
        // z = (95-100)/(10/√25) = -5/2 = -2.5
        var result = _service.ZTest(95, 100, 10, 25, 0.05, "left");

        Assert.True(result.Success);
        Assert.InRange(result.TestStatistic, -2.51, -2.49);
        Assert.True(result.PValue < 0.05);
        Assert.True(result.RejectNull);
    }

    [Fact]
    public void ZTest_RightTailed_DoNotReject()
    {
        // z = (101-100)/(10/√25) = 1/2 = 0.5
        var result = _service.ZTest(101, 100, 10, 25, 0.05, "right");

        Assert.True(result.Success);
        Assert.InRange(result.TestStatistic, 0.49, 0.51);
        Assert.True(result.PValue > 0.05);
        Assert.False(result.RejectNull);
    }

    [Fact]
    public void ZTest_GeneratesCurve()
    {
        var result = _service.ZTest(105, 100, 15, 36, 0.05, "two-tailed");

        Assert.True(result.Success);
        Assert.Equal(200, result.DistributionCurve.Length);
    }

    [Fact]
    public void ZTest_InvalidInputs_ReturnErrors()
    {
        Assert.False(_service.ZTest(100, 100, 0, 10, 0.05, "two-tailed").Success);     // σ = 0
        Assert.False(_service.ZTest(100, 100, 15, 0, 0.05, "two-tailed").Success);     // n = 0
        Assert.False(_service.ZTest(100, 100, 15, 10, 0, "two-tailed").Success);       // α = 0
        Assert.False(_service.ZTest(100, 100, 15, 10, 0.05, "invalid").Success);       // bad alt
    }

    // ════════════════════════════════════════════════════════════════
    //  T-Test
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TTest_KnownExample_TwoTailed()
    {
        // μ₀=50, sample mean=53, s=8, n=20
        // t = (53-50)/(8/√20) = 3/1.789 ≈ 1.677, df=19
        var result = _service.TTest(53, 50, 8, 20, 0.05, "two-tailed");

        Assert.True(result.Success);
        Assert.InRange(result.TestStatistic, 1.67, 1.69);
        Assert.Equal(19, result.DegreesOfFreedom);
        // For df=19, t=1.677 is approximately p=0.11 two-tailed, so do not reject
        Assert.True(result.PValue > 0.05);
        Assert.False(result.RejectNull);
    }

    [Fact]
    public void TTest_LargeStatistic_Rejects()
    {
        // t = (60-50)/(5/√30) ≈ 10.95, df=29 → definitely reject
        var result = _service.TTest(60, 50, 5, 30, 0.05, "two-tailed");

        Assert.True(result.Success);
        Assert.True(result.TestStatistic > 10);
        Assert.True(result.PValue < 0.001);
        Assert.True(result.RejectNull);
    }

    [Fact]
    public void TTest_GeneratesCurve()
    {
        var result = _service.TTest(53, 50, 8, 20, 0.05, "two-tailed");

        Assert.True(result.Success);
        Assert.Equal(200, result.DistributionCurve.Length);
    }

    [Fact]
    public void TTest_InvalidInputs_ReturnErrors()
    {
        Assert.False(_service.TTest(50, 50, 0, 10, 0.05, "two-tailed").Success);   // s = 0
        Assert.False(_service.TTest(50, 50, 5, 1, 0.05, "two-tailed").Success);    // n < 2
        Assert.False(_service.TTest(50, 50, 5, 10, 1.5, "two-tailed").Success);    // α >= 1
    }

    // ════════════════════════════════════════════════════════════════
    //  Chi-Square Test
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ChiSquareTest_FairDie_DoNotReject()
    {
        // Observed counts for a fair die (6 sides, 60 rolls total)
        double[] observed = [10, 9, 11, 10, 12, 8];
        double[] expected = [10, 10, 10, 10, 10, 10];

        var result = _service.ChiSquareTest(observed, expected, 0.05);

        Assert.True(result.Success);
        Assert.Equal(5, result.DegreesOfFreedom);
        Assert.True(result.PValue > 0.05);
        Assert.False(result.RejectNull);
    }

    [Fact]
    public void ChiSquareTest_LoadedDie_Rejects()
    {
        // Clearly skewed results
        double[] observed = [30, 5, 5, 5, 5, 10];
        double[] expected = [10, 10, 10, 10, 10, 10];

        var result = _service.ChiSquareTest(observed, expected, 0.05);

        Assert.True(result.Success);
        Assert.True(result.TestStatistic > 11.07); // critical value for df=5, α=0.05
        Assert.True(result.PValue < 0.05);
        Assert.True(result.RejectNull);
    }

    [Fact]
    public void ChiSquareTest_KnownStatistic()
    {
        // χ² = Σ (O-E)²/E = (10-8)²/8 + (6-8)²/8 = 4/8 + 4/8 = 1.0
        double[] observed = [10, 6];
        double[] expected = [8, 8];

        var result = _service.ChiSquareTest(observed, expected, 0.05);

        Assert.True(result.Success);
        Assert.InRange(result.TestStatistic, 0.99, 1.01);
        Assert.Equal(1, result.DegreesOfFreedom);
    }

    [Fact]
    public void ChiSquareTest_InvalidInputs_ReturnErrors()
    {
        Assert.False(_service.ChiSquareTest([1], [1], 0.05).Success);                // < 2 categories
        Assert.False(_service.ChiSquareTest([1, 2], [1], 0.05).Success);             // mismatched lengths
        Assert.False(_service.ChiSquareTest([1, 2], [0, 2], 0.05).Success);          // expected = 0
        Assert.False(_service.ChiSquareTest([1, 2], [1, 2], 0).Success);             // α = 0
    }

    // ════════════════════════════════════════════════════════════════
    //  MLE Normal
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MleNormal_EstimatesMatchAnalyticalFormulas()
    {
        // Known data: MLE μ = mean, MLE σ = sqrt(Σ(xi-μ)²/n)
        double[] data = [2, 4, 6, 8, 10];
        // mean = 6, σ_MLE = sqrt(40/5) = sqrt(8) ≈ 2.8284

        var result = _service.MleNormal(data);

        Assert.True(result.Success);
        Assert.InRange(result.EstimatedMean, 5.99, 6.01);
        Assert.InRange(result.EstimatedStdDev, 2.82, 2.84);
    }

    [Fact]
    public void MleNormal_GeneratesContourData()
    {
        double[] data = [1, 2, 3, 4, 5];

        var result = _service.MleNormal(data);

        Assert.True(result.Success);
        Assert.True(result.ContourData.Length > 0);
        Assert.Equal(30, result.MuAxis.Length);
        Assert.Equal(30, result.SigmaAxis.Length);
    }

    [Fact]
    public void MleNormal_MaxLogLikelihoodAtMle()
    {
        double[] data = [10, 12, 11, 13, 9, 11];

        var result = _service.MleNormal(data);
        Assert.True(result.Success);

        // The log-likelihood at MLE should be >= any other point in the contour
        double maxContourLL = result.ContourData.Max(c => c.LogLikelihood);
        Assert.True(result.LogLikelihood >= maxContourLL - 0.1);
    }

    [Fact]
    public void MleNormal_InvalidInputs_ReturnErrors()
    {
        Assert.False(_service.MleNormal([1]).Success);                  // < 2 data points
        Assert.False(_service.MleNormal([5, 5, 5, 5]).Success);        // all identical
    }

    // ════════════════════════════════════════════════════════════════
    //  MLE Binomial
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MleBinomial_EstimateIsKOverN()
    {
        var result = _service.MleBinomial(20, 15);

        Assert.True(result.Success);
        Assert.Equal(0.75, result.EstimatedP);
    }

    [Fact]
    public void MleBinomial_GeneratesLogLikelihoodCurve()
    {
        var result = _service.MleBinomial(10, 7);

        Assert.True(result.Success);
        Assert.Equal(200, result.LogLikelihoodCurve.Length);

        // Maximum log-likelihood should be near p = 0.7
        var maxPoint = result.LogLikelihoodCurve.OrderByDescending(p => p.Y).First();
        Assert.InRange(maxPoint.X, 0.65, 0.75);
    }

    [Fact]
    public void MleBinomial_ZeroSuccesses()
    {
        var result = _service.MleBinomial(10, 0);

        Assert.True(result.Success);
        Assert.Equal(0, result.EstimatedP);
    }

    [Fact]
    public void MleBinomial_AllSuccesses()
    {
        var result = _service.MleBinomial(10, 10);

        Assert.True(result.Success);
        Assert.Equal(1, result.EstimatedP);
    }

    [Fact]
    public void MleBinomial_InvalidInputs_ReturnErrors()
    {
        Assert.False(_service.MleBinomial(0, 0).Success);          // trials < 1
        Assert.False(_service.MleBinomial(10, -1).Success);        // successes < 0
        Assert.False(_service.MleBinomial(5, 10).Success);         // successes > trials
    }

    // ════════════════════════════════════════════════════════════════
    //  Confidence Interval
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ConfidenceInterval_ZInterval_KnownSigma()
    {
        // x̄=100, σ=15, n=36, 95% CI
        // z* ≈ 1.96, SE = 15/6 = 2.5, margin = 4.9
        var result = _service.ConfidenceInterval(100, 15, 36, 0.95, isPopulationStdDev: true);

        Assert.True(result.Success);
        Assert.Equal("z", result.IntervalType);
        Assert.InRange(result.CriticalValue, 1.95, 1.97);
        Assert.InRange(result.MarginOfError, 4.8, 5.0);
        Assert.InRange(result.LowerBound, 95.0, 95.2);
        Assert.InRange(result.UpperBound, 104.8, 105.0);
    }

    [Fact]
    public void ConfidenceInterval_TInterval_SmallSample()
    {
        // x̄=50, s=10, n=10, 95% CI → uses t-interval
        var result = _service.ConfidenceInterval(50, 10, 10, 0.95, isPopulationStdDev: false);

        Assert.True(result.Success);
        Assert.Equal("t", result.IntervalType);
        // t*(df=9, 0.975) ≈ 2.262
        Assert.InRange(result.CriticalValue, 2.25, 2.28);
        Assert.True(result.LowerBound < 50);
        Assert.True(result.UpperBound > 50);
    }

    [Fact]
    public void ConfidenceInterval_LargeSampleUseZ_EvenWithSampleStdDev()
    {
        // n=30 with sample std dev → should use z-interval
        var result = _service.ConfidenceInterval(50, 10, 30, 0.95, isPopulationStdDev: false);

        Assert.True(result.Success);
        Assert.Equal("z", result.IntervalType);
    }

    [Fact]
    public void ConfidenceInterval_HigherConfidence_WiderInterval()
    {
        var ci95 = _service.ConfidenceInterval(100, 10, 25, 0.95, true);
        var ci99 = _service.ConfidenceInterval(100, 10, 25, 0.99, true);

        Assert.True(ci95.Success);
        Assert.True(ci99.Success);
        Assert.True(ci99.MarginOfError > ci95.MarginOfError);
    }

    [Fact]
    public void ConfidenceInterval_LargerSample_NarrowerInterval()
    {
        var small = _service.ConfidenceInterval(100, 10, 10, 0.95, true);
        var large = _service.ConfidenceInterval(100, 10, 100, 0.95, true);

        Assert.True(small.Success);
        Assert.True(large.Success);
        Assert.True(large.MarginOfError < small.MarginOfError);
    }

    [Fact]
    public void ConfidenceInterval_InvalidInputs_ReturnErrors()
    {
        Assert.False(_service.ConfidenceInterval(50, 0, 10, 0.95, true).Success);      // σ = 0
        Assert.False(_service.ConfidenceInterval(50, 10, 0, 0.95, true).Success);      // n = 0
        Assert.False(_service.ConfidenceInterval(50, 10, 10, 0, true).Success);        // CL = 0
        Assert.False(_service.ConfidenceInterval(50, 10, 10, 1, true).Success);        // CL = 1
    }

    // ════════════════════════════════════════════════════════════════
    //  Internal Math Helper Spot-Checks
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void StandardNormalCdf_KnownValues()
    {
        // Φ(0) = 0.5
        Assert.InRange(InferenceService.StandardNormalCdf(0), 0.4999, 0.5001);
        // Φ(1.96) ≈ 0.975
        Assert.InRange(InferenceService.StandardNormalCdf(1.96), 0.974, 0.976);
        // Φ(-1.96) ≈ 0.025
        Assert.InRange(InferenceService.StandardNormalCdf(-1.96), 0.024, 0.026);
    }

    [Fact]
    public void StandardNormalQuantile_KnownValues()
    {
        // Φ⁻¹(0.5) = 0
        Assert.InRange(InferenceService.StandardNormalQuantile(0.5), -0.001, 0.001);
        // Φ⁻¹(0.975) ≈ 1.96
        Assert.InRange(InferenceService.StandardNormalQuantile(0.975), 1.95, 1.97);
        // Φ⁻¹(0.025) ≈ -1.96
        Assert.InRange(InferenceService.StandardNormalQuantile(0.025), -1.97, -1.95);
    }

    [Fact]
    public void TDistCdf_ConvergesToNormalForLargeDf()
    {
        // For large df, t-distribution → standard normal
        double normalCdf = InferenceService.StandardNormalCdf(1.96);
        double tCdf = InferenceService.TDistCdf(1.96, 1000);
        Assert.InRange(Math.Abs(normalCdf - tCdf), 0, 0.005);
    }

    [Fact]
    public void BetaPdf_Symmetric_ForEqualAlphaBeta()
    {
        // Beta(5,5) should be symmetric: f(0.3) ≈ f(0.7)
        double left = InferenceService.BetaPdf(0.3, 5, 5);
        double right = InferenceService.BetaPdf(0.7, 5, 5);
        Assert.InRange(Math.Abs(left - right), 0, 0.001);
    }

    [Fact]
    public void ChiSquareCdf_KnownValues()
    {
        // For df=1, χ²=3.841 → CDF ≈ 0.95
        double cdf = InferenceService.ChiSquareCdf(3.841, 1);
        Assert.InRange(cdf, 0.949, 0.951);
    }
}
