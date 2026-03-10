namespace TCalc.Web.Services;

/// <summary>
/// Generates (x, y) data points for mathematical function expressions,
/// inequalities, and conic sections.
/// </summary>
public interface IGraphingService
{
    /// <summary>
    /// Evaluates a function expression over the given x-range and returns data points.
    /// The expression should use 'x' as the variable (e.g. "sin(x)", "x^2+1").
    /// </summary>
    GraphResult GeneratePoints(string expression, double xMin, double xMax, double step);

    /// <summary>
    /// Detects whether the expression contains an inequality operator and, if so,
    /// returns the boundary curve plus fill metadata for the shaded region.
    /// </summary>
    InequalityResult EvaluateInequality(string expression, double xMin, double xMax, double step, double yMin, double yMax);

    /// <summary>
    /// Generates parametric points for a conic section defined by its type and parameters.
    /// </summary>
    ConicResult GenerateConicPoints(string conicType, Dictionary<string, double> parameters);
}

// ─── Standard function result ────────────────────────────────

public sealed class GraphResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string Expression { get; init; } = string.Empty;
    public List<GraphPoint> Points { get; init; } = [];

    // Diagnostics — surfaced so the UI can report evaluation issues
    public int TotalPoints { get; init; }
    public int ErrorCount { get; init; }
    public string? SampleError { get; init; }
    public string? SampleSubstitution { get; init; }
}

public sealed class GraphPoint
{
    public double X { get; init; }
    public double? Y { get; init; }
}

// ─── Inequality result ───────────────────────────────────────

public sealed class InequalityResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public bool IsInequality { get; init; }
    public string Operator { get; init; } = string.Empty;
    public string LeftExpression { get; init; } = string.Empty;
    public string RightExpression { get; init; } = string.Empty;

    /// <summary>Points on the boundary curve (the line of equality).</summary>
    public List<GraphPoint> BoundaryPoints { get; init; } = [];

    /// <summary>
    /// Fill direction relative to the boundary: "above" or "below".
    /// "above" means shade from boundary up to yMax; "below" means from boundary down to yMin.
    /// </summary>
    public string FillDirection { get; init; } = string.Empty;

    /// <summary>Whether the boundary itself is included (≤ or ≥) vs excluded (&lt; or &gt;).</summary>
    public bool BoundaryInclusive { get; init; }
}

// ─── Conic section result ────────────────────────────────────

public sealed class ConicResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string ConicType { get; init; } = string.Empty;
    public string Equation { get; init; } = string.Empty;

    /// <summary>Points for rendering the conic curve (may include multiple series for hyperbolas).</summary>
    public List<List<GraphPoint>> Series { get; init; } = [];

    /// <summary>Named key features (e.g. "Center", "Focus 1", "Vertex") with coordinates.</summary>
    public List<ConicFeature> Features { get; init; } = [];
}

public sealed class ConicFeature
{
    public string Label { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
}
