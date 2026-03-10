using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class CalculatorEngineTests
{
    private readonly CalculatorEngine _engine = new(NullLogger<CalculatorEngine>.Instance);

    // ─── Basic arithmetic ─────────────────────────────────────

    [Theory]
    [InlineData("2+3", 5)]
    [InlineData("10-4", 6)]
    [InlineData("3*7", 21)]
    [InlineData("20/4", 5)]
    [InlineData("0+0", 0)]
    [InlineData("100", 100)]
    public void Evaluate_BasicArithmetic(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Operator precedence ──────────────────────────────────

    [Theory]
    [InlineData("2+3*4", 14)]
    [InlineData("(2+3)*4", 20)]
    [InlineData("10-2*3", 4)]
    [InlineData("10/2+3", 8)]
    [InlineData("2+3*4-1", 13)]
    public void Evaluate_OperatorPrecedence(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Parentheses ──────────────────────────────────────────

    [Theory]
    [InlineData("(1+2)", 3)]
    [InlineData("((3+2))", 5)]
    [InlineData("(2+3)*(4-1)", 15)]
    [InlineData("((2+3)*(4-1))/5", 3)]
    public void Evaluate_Parentheses(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Exponentiation (right-associative) ───────────────────

    [Theory]
    [InlineData("2^3", 8)]
    [InlineData("2^3^2", 512)]   // 2^(3^2) = 2^9 = 512
    [InlineData("4^0.5", 2)]
    [InlineData("10^0", 1)]
    public void Evaluate_Exponentiation(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Unary minus / plus ───────────────────────────────────

    [Theory]
    [InlineData("-5", -5)]
    [InlineData("-(3+2)", -5)]
    [InlineData("-(-3)", 3)]
    [InlineData("+5", 5)]
    [InlineData("2*-3", -6)]
    public void Evaluate_UnaryOperators(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Decimal numbers ──────────────────────────────────────

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData("0.5+0.5", 1)]
    [InlineData("2.5*4", 10)]
    public void Evaluate_Decimals(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Constants ────────────────────────────────────────────

    [Fact]
    public void Evaluate_Pi()
    {
        var result = _engine.Evaluate("pi");
        Assert.True(result.Success);
        Assert.Equal(Math.PI, result.Value, 10);
    }

    [Fact]
    public void Evaluate_E()
    {
        var result = _engine.Evaluate("e");
        Assert.True(result.Success);
        Assert.Equal(Math.E, result.Value, 10);
    }

    [Fact]
    public void Evaluate_PiSymbol()
    {
        var result = _engine.Evaluate("π");
        Assert.True(result.Success);
        Assert.Equal(Math.PI, result.Value, 10);
    }

    // ─── Scientific functions (radians) ───────────────────────

    [Theory]
    [InlineData("sin(0)", 0)]
    [InlineData("cos(0)", 1)]
    [InlineData("tan(0)", 0)]
    [InlineData("ln(1)", 0)]
    [InlineData("log(100)", 2)]
    [InlineData("sqrt(16)", 4)]
    [InlineData("abs(-7)", 7)]
    [InlineData("exp(0)", 1)]
    [InlineData("ceil(2.3)", 3)]
    [InlineData("floor(2.9)", 2)]
    public void Evaluate_ScientificFunctions(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    [Fact]
    public void Evaluate_SinPiOver2()
    {
        var result = _engine.Evaluate("sin(pi/2)");
        Assert.True(result.Success);
        Assert.Equal(1.0, result.Value, 10);
    }

    // ─── Degree mode ──────────────────────────────────────────

    [Fact]
    public void Evaluate_Sin90Degrees()
    {
        var result = _engine.Evaluate("sin(90)", AngleMode.Degrees);
        Assert.True(result.Success);
        Assert.Equal(1.0, result.Value, 10);
    }

    [Fact]
    public void Evaluate_Cos60Degrees()
    {
        var result = _engine.Evaluate("cos(60)", AngleMode.Degrees);
        Assert.True(result.Success);
        Assert.Equal(0.5, result.Value, 10);
    }

    [Fact]
    public void Evaluate_AsinDegrees()
    {
        var result = _engine.Evaluate("asin(1)", AngleMode.Degrees);
        Assert.True(result.Success);
        Assert.Equal(90.0, result.Value, 10);
    }

    // ─── Two-argument functions ───────────────────────────────

    [Theory]
    [InlineData("pow(2,10)", 1024)]
    [InlineData("mod(10,3)", 1)]
    public void Evaluate_BinaryFunctions(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Factorial ────────────────────────────────────────────

    [Theory]
    [InlineData("5!", 120)]
    [InlineData("0!", 1)]
    [InlineData("1!", 1)]
    [InlineData("10!", 3628800)]
    public void Evaluate_Factorial(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    [Fact]
    public void Evaluate_FactorialFunction()
    {
        var result = _engine.Evaluate("factorial(6)");
        Assert.True(result.Success);
        Assert.Equal(720, result.Value, 10);
    }

    // ─── Modulo (%) ───────────────────────────────────────────

    [Theory]
    [InlineData("10%3", 1)]
    [InlineData("7%2", 1)]
    public void Evaluate_Modulo(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── Whitespace handling ──────────────────────────────────

    [Fact]
    public void Evaluate_WithWhitespace()
    {
        var result = _engine.Evaluate("  2  +  3  ");
        Assert.True(result.Success);
        Assert.Equal(5, result.Value);
    }

    // ─── Error cases ──────────────────────────────────────────

    [Fact]
    public void Evaluate_DivisionByZero()
    {
        var result = _engine.Evaluate("1/0");
        Assert.False(result.Success);
        Assert.Contains("infinity", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_ZeroDividedByZero()
    {
        var result = _engine.Evaluate("0/0");
        Assert.False(result.Success);
        Assert.Contains("NaN", result.Error!);
    }

    [Fact]
    public void Evaluate_ModuloByZero()
    {
        var result = _engine.Evaluate("1%0");
        Assert.False(result.Success);
        Assert.Contains("NaN", result.Error!);
    }

    [Fact]
    public void Evaluate_EmptyExpression()
    {
        var result = _engine.Evaluate("");
        Assert.False(result.Success);
    }

    [Fact]
    public void Evaluate_NullExpression()
    {
        var result = _engine.Evaluate(null!);
        Assert.False(result.Success);
    }

    [Fact]
    public void Evaluate_InvalidCharacter()
    {
        var result = _engine.Evaluate("2&3");
        Assert.False(result.Success);
        Assert.Contains("Unexpected", result.Error!);
    }

    [Fact]
    public void Evaluate_MismatchedParenthesis()
    {
        var result = _engine.Evaluate("(2+3");
        Assert.False(result.Success);
        Assert.Contains("parenthesis", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_UnknownFunction()
    {
        var result = _engine.Evaluate("foo(3)");
        Assert.False(result.Success);
        Assert.Contains("Unknown", result.Error!);
    }

    [Fact]
    public void Evaluate_NegativeFactorial()
    {
        var result = _engine.Evaluate("(-3)!");
        Assert.False(result.Success);
        Assert.Contains("negative", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_LargeFactorial()
    {
        var result = _engine.Evaluate("171!");
        Assert.False(result.Success);
        Assert.Contains("too large", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Complex expressions ──────────────────────────────────

    [Theory]
    [InlineData("2+3*4-6/2", 11)]    // 2 + 12 - 3
    [InlineData("sqrt(9)+2^3", 11)]   // 3 + 8
    [InlineData("abs(-5)*2+1", 11)]   // 10 + 1
    [InlineData("(1+2)*(3+4)", 21)]
    public void Evaluate_ComplexExpressions(string expr, double expected)
    {
        var result = _engine.Evaluate(expr);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.Value, 10);
    }

    // ─── EvaluateFunction API ─────────────────────────────────

    [Fact]
    public void EvaluateFunction_Sin()
    {
        double result = _engine.EvaluateFunction("sin", [0]);
        Assert.Equal(0, result, 10);
    }

    [Fact]
    public void EvaluateFunction_Pow()
    {
        double result = _engine.EvaluateFunction("pow", [2, 8]);
        Assert.Equal(256, result, 10);
    }

    [Fact]
    public void EvaluateFunction_Unknown()
    {
        Assert.Throws<CalculatorException>(() => _engine.EvaluateFunction("xyz", [1]));
    }
}
