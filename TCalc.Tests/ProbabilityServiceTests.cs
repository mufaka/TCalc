using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class ProbabilityServiceTests
{
    private readonly ProbabilityService _service = new(NullLogger<ProbabilityService>.Instance);

    // ════════════════════════════════════════════════════════════
    //  Normal Distribution
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Normal_StandardNormal_PdfAtZero()
    {
        var result = _service.Normal(0, 1, 0);

        Assert.True(result.Success);
        Assert.NotNull(result.PdfAtX);
        // PDF of standard normal at 0 ≈ 0.39894228
        Assert.Equal(0.39894228, result.PdfAtX!.Value, 5);
    }

    [Fact]
    public void Normal_StandardNormal_CdfAtZero()
    {
        var result = _service.Normal(0, 1, 0);

        Assert.True(result.Success);
        Assert.NotNull(result.CdfAtX);
        Assert.Equal(0.5, result.CdfAtX!.Value, 4);
    }

    [Fact]
    public void Normal_CdfAt196_IsAbout975()
    {
        var result = _service.Normal(0, 1, 1.96);

        Assert.True(result.Success);
        Assert.NotNull(result.CdfAtX);
        Assert.Equal(0.975, result.CdfAtX!.Value, 2);
    }

    [Fact]
    public void Normal_CdfAtNeg196_IsAbout025()
    {
        var result = _service.Normal(0, 1, -1.96);

        Assert.True(result.Success);
        Assert.NotNull(result.CdfAtX);
        Assert.Equal(0.025, result.CdfAtX!.Value, 2);
    }

    [Fact]
    public void Normal_CustomMeanStdDev()
    {
        var result = _service.Normal(100, 15, 100);

        Assert.True(result.Success);
        Assert.NotNull(result.CdfAtX);
        Assert.Equal(0.5, result.CdfAtX!.Value, 4);
    }

    [Fact]
    public void Normal_WithoutX_ReturnsNullProbabilities()
    {
        var result = _service.Normal(0, 1);

        Assert.True(result.Success);
        Assert.Null(result.PdfAtX);
        Assert.Null(result.CdfAtX);
        Assert.NotEmpty(result.PdfCurve);
    }

    [Fact]
    public void Normal_GeneratesCurvePoints()
    {
        var result = _service.Normal(0, 1);

        Assert.True(result.Success);
        Assert.Equal(200, result.PdfCurve.Length);
        // First point should be around -4σ
        Assert.True(result.PdfCurve[0].X < -3.5);
        // Last point should be around +4σ
        Assert.True(result.PdfCurve[^1].X > 3.5);
    }

    [Fact]
    public void Normal_NegativeStdDev_Fails()
    {
        var result = _service.Normal(0, -1, 0);

        Assert.False(result.Success);
        Assert.Contains("positive", result.Error!);
    }

    [Fact]
    public void Normal_ZeroStdDev_Fails()
    {
        var result = _service.Normal(0, 0, 0);

        Assert.False(result.Success);
    }

    // ════════════════════════════════════════════════════════════
    //  Binomial Distribution
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Binomial_TenTrialsHalfProb_ExpectedValue()
    {
        var result = _service.Binomial(10, 0.5);

        Assert.True(result.Success);
        Assert.Equal(5.0, result.ExpectedValue, 4);
        Assert.Equal(2.5, result.Variance, 4);
    }

    [Fact]
    public void Binomial_PmfAtK5_FairCoin10Flips()
    {
        var result = _service.Binomial(10, 0.5, 5);

        Assert.True(result.Success);
        Assert.NotNull(result.PmfAtK);
        // C(10,5) * 0.5^10 = 252/1024 ≈ 0.24609375
        Assert.Equal(0.24609375, result.PmfAtK!.Value, 5);
    }

    [Fact]
    public void Binomial_CdfAtK10_IsOne()
    {
        var result = _service.Binomial(10, 0.5, 10);

        Assert.True(result.Success);
        Assert.NotNull(result.CdfAtK);
        Assert.Equal(1.0, result.CdfAtK!.Value, 5);
    }

    [Fact]
    public void Binomial_CdfAtK0_IsP0_10()
    {
        var result = _service.Binomial(10, 0.5, 0);

        Assert.True(result.Success);
        Assert.NotNull(result.CdfAtK);
        // (0.5)^10 ≈ 0.0009765625
        Assert.Equal(0.0009765625, result.CdfAtK!.Value, 6);
    }

    [Fact]
    public void Binomial_ProbZero_AllMassAtK0()
    {
        var result = _service.Binomial(10, 0, 0);

        Assert.True(result.Success);
        Assert.NotNull(result.PmfAtK);
        Assert.Equal(1.0, result.PmfAtK!.Value, 5);
    }

    [Fact]
    public void Binomial_ProbOne_AllMassAtKN()
    {
        var result = _service.Binomial(10, 1, 10);

        Assert.True(result.Success);
        Assert.NotNull(result.PmfAtK);
        Assert.Equal(1.0, result.PmfAtK!.Value, 5);
    }

    [Fact]
    public void Binomial_GeneratesPmfPoints()
    {
        var result = _service.Binomial(10, 0.5);

        Assert.True(result.Success);
        Assert.Equal(11, result.PmfPoints.Length); // 0 through 10
    }

    [Fact]
    public void Binomial_NegativeN_Fails()
    {
        var result = _service.Binomial(-1, 0.5);
        Assert.False(result.Success);
    }

    [Fact]
    public void Binomial_POutOfRange_Fails()
    {
        var result = _service.Binomial(10, 1.5);
        Assert.False(result.Success);
    }

    [Fact]
    public void Binomial_KOutOfRange_Fails()
    {
        var result = _service.Binomial(10, 0.5, 11);
        Assert.False(result.Success);
    }

    // ════════════════════════════════════════════════════════════
    //  Poisson Distribution
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Poisson_Lambda5_ExpectedValue()
    {
        var result = _service.Poisson(5);

        Assert.True(result.Success);
        Assert.Equal(5.0, result.ExpectedValue, 4);
        Assert.Equal(5.0, result.Variance, 4);
    }

    [Fact]
    public void Poisson_Lambda5_PmfAtK5()
    {
        var result = _service.Poisson(5, 5);

        Assert.True(result.Success);
        Assert.NotNull(result.PmfAtK);
        // e^(-5) * 5^5 / 5! ≈ 0.17546737
        Assert.Equal(0.17546737, result.PmfAtK!.Value, 4);
    }

    [Fact]
    public void Poisson_Lambda1_PmfAtK0()
    {
        var result = _service.Poisson(1, 0);

        Assert.True(result.Success);
        Assert.NotNull(result.PmfAtK);
        // e^(-1) ≈ 0.36787944
        Assert.Equal(0.36787944, result.PmfAtK!.Value, 5);
    }

    [Fact]
    public void Poisson_CdfSumsToOne_Approximately()
    {
        var result = _service.Poisson(3);

        Assert.True(result.Success);
        double totalPmf = result.PmfPoints.Sum(p => p.Y);
        Assert.True(totalPmf > 0.999, $"Total PMF should be ≈1.0 but was {totalPmf}");
    }

    [Fact]
    public void Poisson_Lambda0_Fails()
    {
        var result = _service.Poisson(0);
        Assert.False(result.Success);
    }

    [Fact]
    public void Poisson_NegativeLambda_Fails()
    {
        var result = _service.Poisson(-5);
        Assert.False(result.Success);
    }

    [Fact]
    public void Poisson_NegativeK_Fails()
    {
        var result = _service.Poisson(5, -1);
        Assert.False(result.Success);
    }

    [Fact]
    public void Poisson_GeneratesPmfPoints()
    {
        var result = _service.Poisson(5);

        Assert.True(result.Success);
        Assert.True(result.PmfPoints.Length > 10);
        Assert.Equal(0, result.PmfPoints[0].X);
    }

    // ════════════════════════════════════════════════════════════
    //  Erf function accuracy
    // ════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0.8427008)]
    [InlineData(-1, -0.8427008)]
    [InlineData(2, 0.9953223)]
    public void Erf_KnownValues(double x, double expected)
    {
        double actual = ProbabilityService.Erf(x);
        Assert.Equal(expected, actual, 4);
    }
}
