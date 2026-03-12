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

    // ─── Multiple linear regression ───────────────────────────────

    [Fact]
    public void MultipleLinearRegression_PerfectFit()
    {
        // y = 1 + 2·x1 + 3·x2 (exact)
        double[][] X = [
            [1, 1],
            [2, 1],
            [1, 2],
            [2, 2],
            [3, 3],
        ];
        double[] y = [6, 8, 9, 11, 16];

        var result = _service.MultipleLinearRegression(X, y);

        Assert.True(result.Success);
        Assert.Equal(3, result.Coefficients.Length); // β₀, β₁, β₂
        Assert.Equal(1.0, result.Coefficients[0], 4); // intercept
        Assert.Equal(2.0, result.Coefficients[1], 4); // x1 coefficient
        Assert.Equal(3.0, result.Coefficients[2], 4); // x2 coefficient
        Assert.True(result.RSquared > 0.999);
    }

    [Fact]
    public void MultipleLinearRegression_ReturnsAdjustedR2()
    {
        double[][] X = [
            [1, 2], [2, 3], [3, 5], [4, 6],
            [5, 8], [6, 9], [7, 11], [8, 12],
        ];
        double[] y = [3.1, 5.0, 8.2, 10.1, 13.2, 15.0, 18.1, 20.2];

        var result = _service.MultipleLinearRegression(X, y);

        Assert.True(result.Success);
        Assert.True(result.RSquared > 0.99);
        Assert.True(result.AdjustedRSquared > 0.98);
        Assert.True(result.AdjustedRSquared <= result.RSquared);
    }

    [Fact]
    public void MultipleLinearRegression_ReturnsStandardErrors()
    {
        double[][] X = [
            [1, 2], [2, 3], [3, 5], [4, 6],
            [5, 8], [6, 9], [7, 11], [8, 12],
        ];
        double[] y = [3.1, 5.0, 8.2, 10.1, 13.2, 15.0, 18.1, 20.2];

        var result = _service.MultipleLinearRegression(X, y);

        Assert.True(result.Success);
        Assert.Equal(3, result.StandardErrors.Length);
        Assert.Equal(3, result.TStatistics.Length);

        // All standard errors should be positive
        foreach (var se in result.StandardErrors)
            Assert.True(se >= 0);
    }

    [Fact]
    public void MultipleLinearRegression_FittedValuesAndResiduals()
    {
        double[][] X = [[1], [2], [3], [4], [5]];
        double[] y = [3, 5, 7, 9, 11]; // y = 2x + 1

        var result = _service.MultipleLinearRegression(X, y);

        Assert.True(result.Success);
        Assert.Equal(5, result.FittedValues.Length);
        Assert.Equal(5, result.Residuals.Length);

        // Residuals should be near zero for perfect linear data
        foreach (var r in result.Residuals)
            Assert.InRange(r, -0.01, 0.01);
    }

    [Fact]
    public void MultipleLinearRegression_GeneratesEquation()
    {
        // Ensure predictors are not collinear
        double[][] X = [[1, 5], [2, 3], [3, 1], [4, 6], [5, 2]];
        double[] y = [10, 8, 5, 16, 9];

        var result = _service.MultipleLinearRegression(X, y);

        Assert.True(result.Success);
        Assert.Contains("ŷ", result.Equation);
        Assert.Contains("x1", result.Equation);
        Assert.Contains("x2", result.Equation);
    }

    [Fact]
    public void MultipleLinearRegression_TooFewObservations_ReturnsError()
    {
        // 2 predictors + intercept = 3 parameters → need at least 4 observations
        double[][] X = [[1, 2], [3, 4], [5, 6]];
        double[] y = [1, 2, 3];

        var result = _service.MultipleLinearRegression(X, y);

        Assert.False(result.Success);
        Assert.Contains("4", result.Error!);
    }

    [Fact]
    public void MultipleLinearRegression_MismatchedLengths_ReturnsError()
    {
        double[][] X = [[1, 2], [3, 4]];
        double[] y = [1, 2, 3];

        var result = _service.MultipleLinearRegression(X, y);

        Assert.False(result.Success);
    }

    [Fact]
    public void MultipleLinearRegression_NullInputs_ReturnsError()
    {
        Assert.False(_service.MultipleLinearRegression(null!, [1, 2]).Success);
        Assert.False(_service.MultipleLinearRegression([[1]], null!).Success);
    }

    [Fact]
    public void MultipleLinearRegression_JaggedRows_ReturnsError()
    {
        double[][] X = [[1, 2], [3]]; // mismatched column counts
        double[] y = [1, 2];

        var result = _service.MultipleLinearRegression(X, y);

        Assert.False(result.Success);
    }

    // ─── Logistic regression ──────────────────────────────────────

    [Fact]
    public void LogisticRegression_SeparableData_HighAccuracy()
    {
        // Mostly separable with slight overlap to avoid perfect-separation divergence
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        bool[] y = [false, false, false, false, true, false, true, true, true, true];

        var result = _service.LogisticRegression(x, y);

        Assert.True(result.Success);
        Assert.True(result.Accuracy >= 0.7);
        Assert.True(result.Beta1 > 0); // positive slope (higher x → higher P)
    }

    [Fact]
    public void LogisticRegression_PredictedProbabilitiesBetween0And1()
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8];
        bool[] y = [false, false, false, true, false, true, true, true];

        var result = _service.LogisticRegression(x, y);

        Assert.True(result.Success);
        Assert.Equal(8, result.PredictedProbabilities.Length);

        foreach (var p in result.PredictedProbabilities)
        {
            Assert.InRange(p, 0, 1);
        }
    }

    [Fact]
    public void LogisticRegression_SigmoidCurveGenerated()
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        bool[] y = [false, false, false, false, true, false, true, true, true, true];

        var result = _service.LogisticRegression(x, y);

        Assert.True(result.Success);
        Assert.True(result.SigmoidCurve.Length > 0);

        // All sigmoid Y values should be in [0, 1]
        foreach (var pt in result.SigmoidCurve)
            Assert.InRange(pt.Y, 0, 1);
    }

    [Fact]
    public void LogisticRegression_SigmoidIsMonotone()
    {
        // Use data with slight overlap to avoid perfect-separation divergence
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        bool[] y = [false, false, false, false, true, false, true, true, true, true];

        var result = _service.LogisticRegression(x, y);

        Assert.True(result.Success);

        // Since Beta1 > 0, sigmoid curve should be monotonically increasing
        if (result.Beta1 > 0)
        {
            for (int i = 1; i < result.SigmoidCurve.Length; i++)
                Assert.True(result.SigmoidCurve[i].Y >= result.SigmoidCurve[i - 1].Y - 1e-10);
        }
    }

    [Fact]
    public void LogisticRegression_AccuracyBetween0And1()
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8];
        bool[] y = [false, false, true, false, true, true, false, true];

        var result = _service.LogisticRegression(x, y);

        Assert.True(result.Success);
        Assert.InRange(result.Accuracy, 0, 1);
    }

    [Fact]
    public void LogisticRegression_TooFewPoints_ReturnsError()
    {
        var result = _service.LogisticRegression([1, 2], [true, false]);
        Assert.False(result.Success);
    }

    [Fact]
    public void LogisticRegression_AllSameOutcome_ReturnsError()
    {
        var result = _service.LogisticRegression([1, 2, 3, 4], [true, true, true, true]);
        Assert.False(result.Success);
    }

    [Fact]
    public void LogisticRegression_MismatchedLengths_ReturnsError()
    {
        var result = _service.LogisticRegression([1, 2, 3], [true, false]);
        Assert.False(result.Success);
    }

    [Fact]
    public void LogisticRegression_NullInputs_ReturnsError()
    {
        Assert.False(_service.LogisticRegression(null!, [true, false, true]).Success);
        Assert.False(_service.LogisticRegression([1, 2, 3], null!).Success);
    }
}
