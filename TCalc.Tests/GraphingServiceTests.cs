using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class GraphingServiceTests
{
    private readonly GraphingService _service = new(
        new CalculatorEngine(NullLogger<CalculatorEngine>.Instance),
        NullLogger<GraphingService>.Instance);

    [Fact]
    public void GeneratePoints_LinearFunction_ReturnsCorrectPoints()
    {
        var result = _service.GeneratePoints("x", -2, 2, 1);

        Assert.True(result.Success);
        Assert.Equal(5, result.Points.Count); // -2, -1, 0, 1, 2
        Assert.Equal(-2, result.Points[0].Y);
        Assert.Equal(0, result.Points[2].Y);
        Assert.Equal(2, result.Points[4].Y);
    }

    [Fact]
    public void GeneratePoints_QuadraticFunction()
    {
        var result = _service.GeneratePoints("x^2", -2, 2, 1);

        Assert.True(result.Success);
        Assert.Equal(4.0, result.Points[0].Y);  // (-2)^2 = 4
        Assert.Equal(0.0, result.Points[2].Y);  // 0^2 = 0
        Assert.Equal(4.0, result.Points[4].Y);  // 2^2 = 4
    }

    [Fact]
    public void GeneratePoints_SinFunction()
    {
        var result = _service.GeneratePoints("sin(x)", 0, Math.PI, Math.PI / 2);

        Assert.True(result.Success);
        Assert.True(result.Points.Count >= 3);
        Assert.Equal(0.0, result.Points[0].Y!.Value, 10);   // sin(0) = 0
        Assert.Equal(1.0, result.Points[1].Y!.Value, 10);   // sin(π/2) = 1
    }

    [Fact]
    public void GeneratePoints_EmptyExpression_ReturnsFail()
    {
        var result = _service.GeneratePoints("", -1, 1, 0.5);

        Assert.False(result.Success);
        Assert.Contains("empty", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GeneratePoints_InvalidRange_ReturnsFail()
    {
        var result = _service.GeneratePoints("x", 5, -5, 1);

        Assert.False(result.Success);
        Assert.Contains("xMin", result.Error!);
    }

    [Fact]
    public void GeneratePoints_ZeroStep_ReturnsFail()
    {
        var result = _service.GeneratePoints("x", -1, 1, 0);

        Assert.False(result.Success);
        Assert.Contains("Step", result.Error!);
    }

    [Fact]
    public void GeneratePoints_UndefinedPoint_ReturnsNullY()
    {
        // 1/x is undefined at x=0 (infinity), should be null Y
        var result = _service.GeneratePoints("1/x", -1, 1, 1);

        Assert.True(result.Success);
        var zeroPoint = result.Points.FirstOrDefault(p => p.X == 0);
        Assert.NotNull(zeroPoint);
        Assert.Null(zeroPoint.Y); // undefined at x=0

        // Diagnostics should report the error
        Assert.Equal(1, result.ErrorCount);
        Assert.NotNull(result.SampleError);
        Assert.NotNull(result.SampleSubstitution);
    }

    [Fact]
    public void GeneratePoints_AllPointsFail_ReturnsNotSuccess()
    {
        // "gibberish" is not a valid expression — all points should fail
        var result = _service.GeneratePoints("gibberish", -1, 1, 1);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.True(result.ErrorCount > 0);
        Assert.Equal(result.TotalPoints, result.ErrorCount);
        Assert.NotNull(result.SampleError);
        Assert.NotNull(result.SampleSubstitution);
    }

    [Fact]
    public void GeneratePoints_ConstantExpression_NoDiagnosticErrors()
    {
        var result = _service.GeneratePoints("5", -2, 2, 1);

        Assert.True(result.Success);
        Assert.All(result.Points, p => Assert.Equal(5.0, p.Y));
        Assert.Equal(0, result.ErrorCount);
        Assert.Null(result.SampleError);
    }

    [Fact]
    public void GeneratePoints_StopsAtMaximumPointLimit()
    {
        var result = _service.GeneratePoints("x", 0, 20_000, 1);

        Assert.True(result.Success);
        Assert.Equal(10_000, result.TotalPoints);
        Assert.Equal(10_000, result.Points.Count);
        Assert.Equal(0.0, result.Points[0].X);
        Assert.Equal(9_999.0, result.Points[^1].X);
        Assert.Equal(9_999.0, result.Points[^1].Y);
    }

    [Fact]
    public void SubstituteX_DoesNotReplaceInsideFunctionNames()
    {
        // "exp(x)" should not become "e(value)p(value)"
        string result = GraphingService.SubstituteX("exp(x)", 2);
        Assert.Equal("exp((2))", result);
    }

    [Fact]
    public void SubstituteX_ReplacesUppercaseX()
    {
        string result = GraphingService.SubstituteX("X+x", 3);
        Assert.Equal("(3)+(3)", result);
    }

    [Fact]
    public void SubstituteX_HandlesNegativeValues()
    {
        string result = GraphingService.SubstituteX("x+1", -3);
        Assert.Equal("(-3)+1", result);
    }

    [Fact]
    public void SubstituteX_MultipleOccurrences()
    {
        string result = GraphingService.SubstituteX("x*x+x", 5);
        Assert.Equal("(5)*(5)+(5)", result);
    }

    [Fact]
    public void SubstituteX_SmallDecimal_NoConatenation()
    {
        // 2x with x=0.04 must not become "20.04"
        string result = GraphingService.SubstituteX("2x", 0.04);
        Assert.Equal("2(0.04)", result);
    }

    // ─── §6A Inequality detection ─────────────────────────────

    [Fact]
    public void EvaluateInequality_NoOperator_ReturnsNotInequality()
    {
        var result = _service.EvaluateInequality("x^2", -5, 5, 0.5, -10, 10);
        Assert.True(result.Success);
        Assert.False(result.IsInequality);
    }

    [Fact]
    public void EvaluateInequality_LessThan_DetectsAndReturnsBelow()
    {
        var result = _service.EvaluateInequality("y < x^2", -2, 2, 1, -10, 10);

        Assert.True(result.Success);
        Assert.True(result.IsInequality);
        Assert.Equal("<", result.Operator);
        Assert.Equal("below", result.FillDirection);
        Assert.False(result.BoundaryInclusive);
        Assert.True(result.BoundaryPoints.Count > 0);
    }

    [Fact]
    public void EvaluateInequality_GreaterThanOrEqual_DetectsAndReturnsAbove()
    {
        var result = _service.EvaluateInequality("y >= x+1", -2, 2, 1, -10, 10);

        Assert.True(result.Success);
        Assert.True(result.IsInequality);
        Assert.Equal(">=", result.Operator);
        Assert.Equal("above", result.FillDirection);
        Assert.True(result.BoundaryInclusive);
    }

    [Fact]
    public void EvaluateInequality_EmptyExpression_Fails()
    {
        var result = _service.EvaluateInequality("", -5, 5, 0.5, -10, 10);
        Assert.False(result.Success);
    }

    [Fact]
    public void EvaluateInequality_ZeroStep_UsesFallbackSampling()
    {
        var result = _service.EvaluateInequality("y < x", -1, 1, 0, -10, 10);

        Assert.True(result.Success);
        Assert.True(result.IsInequality);
        Assert.Equal("<", result.Operator);
        Assert.True(result.BoundaryPoints.Count >= 500);
    }

    [Fact]
    public void FindInequalityOperator_SkipsInsideParentheses()
    {
        // The < inside parentheses should not be found
        int idx = GraphingService.FindInequalityOperator("max(x, 3) < 5", "<");
        Assert.True(idx > 0); // should find the outer <
        // The inner comma-delimited stuff shouldn't confuse it, but actually there's
        // no < inside the parens here. Let's verify the outer one.
        Assert.Equal("max(x, 3) ".Length, idx);
    }

    [Fact]
    public void FindInequalityOperator_IgnoresOperatorsInsideParentheses()
    {
        int idx = GraphingService.FindInequalityOperator("(x < 3)", "<");
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void EvaluateInequality_LessThanOrEqual_Inclusive()
    {
        var result = _service.EvaluateInequality("y <= sin(x)", -3, 3, 0.5, -10, 10);

        Assert.True(result.Success);
        Assert.True(result.IsInequality);
        Assert.Equal("<=", result.Operator);
        Assert.True(result.BoundaryInclusive);
        Assert.Equal("below", result.FillDirection);
    }

    [Fact]
    public void EvaluateInequality_NonYLeftSide_ReturnsSplitExpressionsAndBoundary()
    {
        var result = _service.EvaluateInequality("x^2 < x+1", -1, 1, 1, -10, 10);

        Assert.True(result.Success);
        Assert.True(result.IsInequality);
        Assert.Equal("x^2", result.LeftExpression);
        Assert.Equal("x+1", result.RightExpression);
        Assert.Equal("above", result.FillDirection);
        Assert.Collection(
            result.BoundaryPoints,
            point =>
            {
                Assert.Equal(-1.0, point.X);
                Assert.Equal(0.0, point.Y);
            },
            point =>
            {
                Assert.Equal(0.0, point.X);
                Assert.Equal(1.0, point.Y);
            },
            point =>
            {
                Assert.Equal(1.0, point.X);
                Assert.Equal(2.0, point.Y);
            });
    }

    // ─── §6C Conic section generation ─────────────────────────

    [Fact]
    public void GenerateConicPoints_Circle_CorrectPoints()
    {
        var result = _service.GenerateConicPoints("circle", new() { ["h"] = 0, ["k"] = 0, ["r"] = 5 });

        Assert.True(result.Success);
        Assert.Equal("circle", result.ConicType);
        Assert.Single(result.Series);
        Assert.True(result.Series[0].Count > 100);
        Assert.Contains(result.Features, f => f.Label == "Center");
        Assert.NotNull(result.Equation);
        Assert.NotEmpty(result.Equation);
    }

    [Fact]
    public void GenerateConicPoints_Circle_NegativeRadius_Fails()
    {
        var result = _service.GenerateConicPoints("circle", new() { ["r"] = -1 });
        Assert.False(result.Success);
        Assert.Contains("positive", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateConicPoints_Circle_UsesDefaultParameters()
    {
        var result = _service.GenerateConicPoints("circle", new());

        Assert.True(result.Success);
        Assert.Equal("circle", result.ConicType);
        Assert.Equal("(x − 0)² + (y − 0)² = 1", result.Equation);
        Assert.Equal(1.0, result.Series[0][0].X);
        Assert.Equal(0.0, result.Series[0][0].Y);
        Assert.Contains(result.Features, f => f.Label == "Center" && f.X == 0 && f.Y == 0);
    }

    [Fact]
    public void GenerateConicPoints_Ellipse_HasFoci()
    {
        var result = _service.GenerateConicPoints("ellipse", new() { ["h"] = 0, ["k"] = 0, ["a"] = 5, ["b"] = 3 });

        Assert.True(result.Success);
        Assert.Equal("ellipse", result.ConicType);
        Assert.Contains(result.Features, f => f.Label == "Focus 1");
        Assert.Contains(result.Features, f => f.Label == "Focus 2");
        Assert.Contains(result.Features, f => f.Label == "Center");
    }

    [Fact]
    public void GenerateConicPoints_Ellipse_VerticalMajorAxis_FociLieOnYAxis()
    {
        var result = _service.GenerateConicPoints("ellipse", new() { ["h"] = 0, ["k"] = 0, ["a"] = 3, ["b"] = 5 });

        Assert.True(result.Success);
        Assert.Contains(result.Features, f => f.Label == "Focus 1" && f.X == 0 && f.Y == 4);
        Assert.Contains(result.Features, f => f.Label == "Focus 2" && f.X == 0 && f.Y == -4);
    }

    [Fact]
    public void GenerateConicPoints_Parabola_HasVertexAndFocus()
    {
        var result = _service.GenerateConicPoints("parabola", new() { ["h"] = 0, ["k"] = 0, ["a"] = 1 });

        Assert.True(result.Success);
        Assert.Equal("parabola", result.ConicType);
        Assert.Contains(result.Features, f => f.Label == "Vertex");
        Assert.Contains(result.Features, f => f.Label == "Focus");
        Assert.Contains(result.Features, f => f.Label.StartsWith("Directrix"));
    }

    [Fact]
    public void GenerateConicPoints_Parabola_NegativeCoefficient_PlacesFocusBelowVertex()
    {
        var result = _service.GenerateConicPoints("parabola", new() { ["h"] = 2, ["k"] = 3, ["a"] = -1 });

        Assert.True(result.Success);
        Assert.Contains(result.Features, f => f.Label == "Vertex" && f.X == 2 && f.Y == 3);
        Assert.Contains(result.Features, f => f.Label == "Focus" && f.X == 2 && f.Y == 2.75);
        Assert.Contains(result.Features, f => f.Label == "Directrix (y)" && f.X == 2 && f.Y == 3.25);
    }

    [Fact]
    public void GenerateConicPoints_Parabola_ZeroCoefficient_Fails()
    {
        var result = _service.GenerateConicPoints("parabola", new() { ["a"] = 0 });
        Assert.False(result.Success);
    }

    [Fact]
    public void GenerateConicPoints_Hyperbola_TwoBranches()
    {
        var result = _service.GenerateConicPoints("hyperbola", new() { ["h"] = 0, ["k"] = 0, ["a"] = 3, ["b"] = 2 });

        Assert.True(result.Success);
        Assert.Equal("hyperbola", result.ConicType);
        Assert.Equal(2, result.Series.Count); // right branch + left branch
        Assert.Contains(result.Features, f => f.Label == "Vertex 1");
        Assert.Contains(result.Features, f => f.Label == "Vertex 2");
    }

    [Fact]
    public void GenerateConicPoints_Hyperbola_ComputesFocusPositions()
    {
        var result = _service.GenerateConicPoints("hyperbola", new() { ["h"] = 0, ["k"] = 0, ["a"] = 3, ["b"] = 4 });

        Assert.True(result.Success);
        Assert.Contains(result.Features, f => f.Label == "Focus 1" && f.X == 5 && f.Y == 0);
        Assert.Contains(result.Features, f => f.Label == "Focus 2" && f.X == -5 && f.Y == 0);
    }

    [Fact]
    public void GenerateConicPoints_UnknownType_Fails()
    {
        var result = _service.GenerateConicPoints("triangle", new());
        Assert.False(result.Success);
        Assert.Contains("Unknown", result.Error!);
    }

    [Fact]
    public void GenerateConicPoints_EmptyType_Fails()
    {
        var result = _service.GenerateConicPoints("", new());
        Assert.False(result.Success);
    }
}
