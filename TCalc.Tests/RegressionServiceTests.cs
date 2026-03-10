using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class RegressionServiceTests
{
    private readonly RegressionService _service = new(NullLogger<RegressionService>.Instance);

    // ─── Linear regression ────────────────────────────────────

    [Fact]
    public void LinearRegression_PerfectFit()
    {
        // y = 2x + 1 (exact)
        double[] x = [1, 2, 3, 4, 5];
        double[] y = [3, 5, 7, 9, 11];

        var result = _service.LinearRegression(x, y);

        Assert.True(result.Success);
        Assert.Equal(2, result.Coefficients.Length);
        Assert.Equal(1.0, result.Coefficients[0], 6);  // intercept
        Assert.Equal(2.0, result.Coefficients[1], 6);  // slope
        Assert.Equal(1.0, result.RSquared, 6);          // perfect fit
        Assert.Contains("x", result.Equation);
    }

    [Fact]
    public void LinearRegression_ImperfectFit()
    {
        double[] x = [1, 2, 3, 4, 5];
        double[] y = [2.1, 4.0, 5.9, 8.1, 10.2];

        var result = _service.LinearRegression(x, y);

        Assert.True(result.Success);
        Assert.True(result.RSquared > 0.99);  // very good fit
        Assert.True(result.FittedPoints.Length > 0);
    }

    [Fact]
    public void LinearRegression_TwoPoints()
    {
        // Minimum viable regression
        double[] x = [0, 10];
        double[] y = [5, 25];

        var result = _service.LinearRegression(x, y);

        Assert.True(result.Success);
        Assert.Equal(5.0, result.Coefficients[0], 6);  // intercept = 5
        Assert.Equal(2.0, result.Coefficients[1], 6);  // slope = 2
        Assert.Equal(1.0, result.RSquared, 6);
    }

    [Fact]
    public void LinearRegression_InsufficientData()
    {
        var result = _service.LinearRegression([1], [2]);

        Assert.False(result.Success);
        Assert.Contains("2", result.Error!);
    }

    [Fact]
    public void LinearRegression_MismatchedLengths()
    {
        var result = _service.LinearRegression([1, 2, 3], [1, 2]);

        Assert.False(result.Success);
        Assert.Contains("equal", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Polynomial regression ────────────────────────────────

    [Fact]
    public void PolynomialRegression_Quadratic_PerfectFit()
    {
        // y = x² + 1
        double[] x = [-2, -1, 0, 1, 2, 3];
        double[] y = [5, 2, 1, 2, 5, 10];

        var result = _service.PolynomialRegression(x, y, degree: 2);

        Assert.True(result.Success);
        Assert.Equal(3, result.Coefficients.Length);
        Assert.Equal(1.0, result.Coefficients[0], 4);  // c0 = 1
        Assert.True(Math.Abs(result.Coefficients[1]) < 0.01);  // c1 ≈ 0
        Assert.Equal(1.0, result.Coefficients[2], 4);  // c2 = 1
        Assert.True(result.RSquared > 0.999);
    }

    [Fact]
    public void PolynomialRegression_DegreeZero_Fails()
    {
        var result = _service.PolynomialRegression([1, 2, 3], [1, 2, 3], degree: 0);

        Assert.False(result.Success);
        Assert.Contains("between", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PolynomialRegression_DegreeExceedsMax_Fails()
    {
        var result = _service.PolynomialRegression([1, 2, 3], [1, 2, 3], degree: 11);

        Assert.False(result.Success);
    }

    [Fact]
    public void PolynomialRegression_NotEnoughPoints()
    {
        // Degree 3 needs at least 4 points
        var result = _service.PolynomialRegression([1, 2, 3], [1, 4, 9], degree: 3);

        Assert.False(result.Success);
        Assert.Contains("4", result.Error!);
    }

    // ─── EvaluatePolynomial ───────────────────────────────────

    [Fact]
    public void EvaluatePolynomial_Linear()
    {
        // 3 + 2x at x=5 → 13
        double result = RegressionService.EvaluatePolynomial([3, 2], 5);
        Assert.Equal(13, result, 10);
    }

    [Fact]
    public void EvaluatePolynomial_Quadratic()
    {
        // 1 + 0x + 1x² at x=3 → 10
        double result = RegressionService.EvaluatePolynomial([1, 0, 1], 3);
        Assert.Equal(10, result, 10);
    }

    // ─── FormatEquation ───────────────────────────────────────

    [Fact]
    public void FormatEquation_Linear()
    {
        string eq = RegressionService.FormatEquation([1.5, 2.0]);
        Assert.Contains("y =", eq);
        Assert.Contains("x", eq);
    }

    [Fact]
    public void FormatEquation_Quadratic()
    {
        string eq = RegressionService.FormatEquation([1, 0, 3]);
        Assert.Contains("x^2", eq);
    }
}
