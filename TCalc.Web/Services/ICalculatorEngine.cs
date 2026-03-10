namespace TCalc.Web.Services;

/// <summary>
/// Parses and evaluates mathematical expressions.
/// </summary>
public interface ICalculatorEngine
{
    /// <summary>
    /// Evaluates an infix mathematical expression and returns the result.
    /// Supports +, -, *, /, ^, parentheses, and scientific functions.
    /// </summary>
    CalculationResult Evaluate(string expression, AngleMode angleMode = AngleMode.Radians);

    /// <summary>
    /// Evaluates a named mathematical function with the given arguments.
    /// </summary>
    double EvaluateFunction(string name, double[] args, AngleMode angleMode = AngleMode.Radians);
}

public enum AngleMode
{
    Radians,
    Degrees
}

public sealed class CalculationResult
{
    public bool Success { get; init; }
    public double Value { get; init; }
    public string? Error { get; init; }
    public string Expression { get; init; } = string.Empty;

    public static CalculationResult Ok(double value, string expression) =>
        new() { Success = true, Value = value, Expression = expression };

    public static CalculationResult Fail(string error, string expression) =>
        new() { Success = false, Error = error, Expression = expression };
}
