namespace TCalc.Web.Services;

/// <summary>
/// Generates data points by substituting x values into a function expression
/// and evaluating with <see cref="ICalculatorEngine"/>.
/// </summary>
public sealed class GraphingService : IGraphingService
{
    private readonly ICalculatorEngine _engine;
    private readonly ILogger<GraphingService> _logger;

    // Inequality operators ordered longest-first so we match "<=" before "<"
    private static readonly string[] InequalityOps = ["<=", ">=", "<", ">"];

    public GraphingService(ICalculatorEngine engine, ILogger<GraphingService> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public GraphResult GeneratePoints(string expression, double xMin, double xMax, double step)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new GraphResult { Success = false, Error = "Expression is empty.", Expression = expression };

        if (step <= 0)
            return new GraphResult { Success = false, Error = "Step must be positive.", Expression = expression };

        if (xMin >= xMax)
            return new GraphResult { Success = false, Error = "xMin must be less than xMax.", Expression = expression };

        var points = new List<GraphPoint>();
        int maxPoints = 10_000;
        int errorCount = 0;
        string? sampleError = null;
        string? sampleSubstitution = null;

        for (double x = xMin; x <= xMax && points.Count < maxPoints; x += step)
        {
            // Replace 'x' in the expression with the current value.
            // We wrap the value in parentheses to handle negative numbers correctly.
            string substituted = SubstituteX(expression, x);
            var result = _engine.Evaluate(substituted);

            if (result.Success && double.IsFinite(result.Value))
            {
                points.Add(new GraphPoint { X = Math.Round(x, 10), Y = result.Value });
            }
            else
            {
                errorCount++;
                // Capture first error details for diagnostics
                if (sampleError is null)
                {
                    sampleError = result.Error;
                    sampleSubstitution = substituted;
                }
                // Discontinuity or undefined point — add null Y so the chart can break the line
                points.Add(new GraphPoint { X = Math.Round(x, 10), Y = null });
            }
        }

        int totalPoints = points.Count;
        bool allFailed = errorCount == totalPoints && totalPoints > 0;

        if (allFailed)
            _logger.LogWarning("Graph generation failed for '{Expression}': all {Count} points errored. First error: {Error}", expression, totalPoints, sampleError);
        else if (errorCount > 0)
            _logger.LogDebug("Graph '{Expression}': {Total} points, {Errors} errors", expression, totalPoints, errorCount);

        return new GraphResult
        {
            Success = !allFailed,
            Error = allFailed ? $"All {totalPoints} points failed to evaluate. First error: {sampleError}" : null,
            Expression = expression,
            Points = points,
            TotalPoints = totalPoints,
            ErrorCount = errorCount,
            SampleError = sampleError,
            SampleSubstitution = sampleSubstitution
        };
    }

    /// <summary>
    /// Replaces the variable 'x' (case-insensitive, whole-word) with the numeric value.
    /// Avoids replacing 'x' inside function names like "exp" or "max".
    /// </summary>
    internal static string SubstituteX(string expression, double value)
    {
        // Always wrap in parentheses to prevent concatenation bugs (e.g. 2x with x=0.04 → 2(0.04) not 20.04)
        string replacement = $"({value.ToString(System.Globalization.CultureInfo.InvariantCulture)})";

        var result = new System.Text.StringBuilder(expression.Length + 16);
        for (int i = 0; i < expression.Length; i++)
        {
            if ((expression[i] == 'x' || expression[i] == 'X') && !IsPartOfIdentifier(expression, i))
            {
                result.Append(replacement);
            }
            else
            {
                result.Append(expression[i]);
            }
        }
        return result.ToString();
    }

    private static bool IsPartOfIdentifier(string expr, int pos)
    {
        // Check if the previous or next character is a letter (meaning this 'x' is part of a word like "exp", "max")
        if (pos > 0 && char.IsLetter(expr[pos - 1]))
            return true;
        if (pos + 1 < expr.Length && char.IsLetter(expr[pos + 1]))
            return true;
        return false;
    }

    // ────────────────────────────────────────────────────────────
    //  §6A — Inequality support
    // ────────────────────────────────────────────────────────────

    public InequalityResult EvaluateInequality(string expression, double xMin, double xMax, double step, double yMin, double yMax)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new InequalityResult { Success = false, Error = "Expression is empty." };

        // Detect the inequality operator
        string? detectedOp = null;
        int opIndex = -1;
        foreach (var op in InequalityOps)
        {
            int idx = FindInequalityOperator(expression, op);
            if (idx >= 0)
            {
                detectedOp = op;
                opIndex = idx;
                break;
            }
        }

        if (detectedOp is null)
            return new InequalityResult { Success = true, IsInequality = false };

        string left = expression[..opIndex].Trim();
        string right = expression[(opIndex + detectedOp.Length)..].Trim();

        // Normalise to "f(x) <op> g(x)" → boundary = g(x) - f(x) ... or simpler: evaluate both sides
        // Typical form: "y < f(x)" or "f(x) > g(x)"
        // If left side is just "y", the boundary is the right expression.
        // Otherwise we rearrange: left <op> right  →  boundary = right - left if op is < or <=,
        //   or left - right if > or >=, and fill is always "below" that boundary for the < family.

        bool leftIsY = left.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
        string boundaryExpr;
        string fillDirection;
        bool inclusive = detectedOp is "<=" or ">=";

        if (leftIsY)
        {
            // y < f(x)  → boundary is f(x), fill below
            // y > f(x)  → boundary is f(x), fill above
            boundaryExpr = right;
            fillDirection = detectedOp is "<" or "<=" ? "below" : "above";
        }
        else
        {
            // f(x) < g(x) → boundary is g(x), shade below g(x) [where f(x) satisfies]
            // We'll plot the right-hand side as boundary and shade:
            //   f(x) < g(x) same as y < g(x) when y = f(x), but for simplicity
            //   we treat: boundary = (right), fill below for '<' ops
            //   f(x) > g(x) → boundary = right, fill above
            boundaryExpr = right;
            fillDirection = detectedOp is "<" or "<=" ? "above" : "below";
        }

        _logger.LogDebug("Inequality detected: '{Expr}' → operator={Op}, boundary='{Boundary}', fill={Fill}",
            expression, detectedOp, boundaryExpr, fillDirection);

        if (step <= 0) step = (xMax - xMin) / 500.0;
        var graphResult = GeneratePoints(boundaryExpr, xMin, xMax, step);
        if (!graphResult.Success)
            return new InequalityResult { Success = false, Error = graphResult.Error };

        return new InequalityResult
        {
            Success = true,
            IsInequality = true,
            Operator = detectedOp,
            LeftExpression = left,
            RightExpression = right,
            BoundaryPoints = graphResult.Points,
            FillDirection = fillDirection,
            BoundaryInclusive = inclusive
        };
    }

    /// <summary>
    /// Finds an inequality operator that is NOT inside parentheses.
    /// Returns the char index of the first occurrence, or -1.
    /// </summary>
    internal static int FindInequalityOperator(string expr, string op)
    {
        int depth = 0;
        for (int i = 0; i < expr.Length; i++)
        {
            if (expr[i] == '(') { depth++; continue; }
            if (expr[i] == ')') { depth--; continue; }
            if (depth == 0 && i + op.Length <= expr.Length && expr.Substring(i, op.Length) == op)
                return i;
        }
        return -1;
    }

    // ────────────────────────────────────────────────────────────
    //  §6C — Conic section support
    // ────────────────────────────────────────────────────────────

    public ConicResult GenerateConicPoints(string conicType, Dictionary<string, double> parameters)
    {
        if (string.IsNullOrWhiteSpace(conicType))
            return new ConicResult { Success = false, Error = "Conic type is required." };

        _logger.LogDebug("Generating conic section: type={Type}, params={@Params}", conicType, parameters);

        return conicType.ToLowerInvariant() switch
        {
            "circle" => GenerateCircle(parameters),
            "ellipse" => GenerateEllipse(parameters),
            "parabola" => GenerateParabola(parameters),
            "hyperbola" => GenerateHyperbola(parameters),
            _ => new ConicResult { Success = false, Error = $"Unknown conic type: '{conicType}'." }
        };
    }

    private static ConicResult GenerateCircle(Dictionary<string, double> p)
    {
        double h = p.GetValueOrDefault("h", 0);
        double k = p.GetValueOrDefault("k", 0);
        double r = p.GetValueOrDefault("r", 1);
        if (r <= 0) return new ConicResult { Success = false, Error = "Radius must be positive." };

        const int n = 360;
        var pts = new List<GraphPoint>(n + 1);
        for (int i = 0; i <= n; i++)
        {
            double theta = 2 * Math.PI * i / n;
            pts.Add(new GraphPoint { X = Math.Round(h + r * Math.Cos(theta), 8), Y = Math.Round(k + r * Math.Sin(theta), 8) });
        }

        return new ConicResult
        {
            Success = true,
            ConicType = "circle",
            Equation = $"(x − {h})² + (y − {k})² = {r * r:G6}",
            Series = [pts],
            Features =
            [
                new ConicFeature { Label = "Center", X = h, Y = k }
            ]
        };
    }

    private static ConicResult GenerateEllipse(Dictionary<string, double> p)
    {
        double h = p.GetValueOrDefault("h", 0);
        double k = p.GetValueOrDefault("k", 0);
        double a = p.GetValueOrDefault("a", 2);
        double b = p.GetValueOrDefault("b", 1);
        if (a <= 0 || b <= 0) return new ConicResult { Success = false, Error = "Semi-axes a and b must be positive." };

        const int n = 360;
        var pts = new List<GraphPoint>(n + 1);
        for (int i = 0; i <= n; i++)
        {
            double theta = 2 * Math.PI * i / n;
            pts.Add(new GraphPoint { X = Math.Round(h + a * Math.Cos(theta), 8), Y = Math.Round(k + b * Math.Sin(theta), 8) });
        }

        double c = Math.Sqrt(Math.Abs(a * a - b * b));
        var features = new List<ConicFeature>
        {
            new() { Label = "Center", X = h, Y = k },
            new() { Label = "Vertex 1", X = h + a, Y = k },
            new() { Label = "Vertex 2", X = h - a, Y = k },
        };
        if (a >= b)
        {
            features.Add(new ConicFeature { Label = "Focus 1", X = Math.Round(h + c, 6), Y = k });
            features.Add(new ConicFeature { Label = "Focus 2", X = Math.Round(h - c, 6), Y = k });
        }
        else
        {
            features.Add(new ConicFeature { Label = "Focus 1", X = h, Y = Math.Round(k + c, 6) });
            features.Add(new ConicFeature { Label = "Focus 2", X = h, Y = Math.Round(k - c, 6) });
        }

        return new ConicResult
        {
            Success = true,
            ConicType = "ellipse",
            Equation = $"(x − {h})²/{a * a:G6} + (y − {k})²/{b * b:G6} = 1",
            Series = [pts],
            Features = features
        };
    }

    private static ConicResult GenerateParabola(Dictionary<string, double> p)
    {
        double h = p.GetValueOrDefault("h", 0);
        double k = p.GetValueOrDefault("k", 0);
        double a = p.GetValueOrDefault("a", 1);
        if (a == 0) return new ConicResult { Success = false, Error = "Parameter 'a' must be non-zero." };

        // y = a(x-h)² + k  →  vertex form
        // Focus at (h, k + 1/(4a)), directrix y = k - 1/(4a)
        double focusY = k + 1.0 / (4.0 * a);
        double directrixY = k - 1.0 / (4.0 * a);

        const int n = 400;
        var pts = new List<GraphPoint>(n + 1);
        double span = 10.0 / Math.Sqrt(Math.Abs(a)); // adaptive range based on curvature
        double xMin = h - span;
        double xMax = h + span;
        double step = (xMax - xMin) / n;

        for (double x = xMin; x <= xMax; x += step)
        {
            double dx = x - h;
            double y = a * dx * dx + k;
            pts.Add(new GraphPoint { X = Math.Round(x, 8), Y = Math.Round(y, 8) });
        }

        return new ConicResult
        {
            Success = true,
            ConicType = "parabola",
            Equation = $"y = {a:G6}(x − {h})² + {k}",
            Series = [pts],
            Features =
            [
                new ConicFeature { Label = "Vertex", X = h, Y = k },
                new ConicFeature { Label = "Focus", X = h, Y = Math.Round(focusY, 6) },
                new ConicFeature { Label = "Directrix (y)", X = h, Y = Math.Round(directrixY, 6) }
            ]
        };
    }

    private static ConicResult GenerateHyperbola(Dictionary<string, double> p)
    {
        double h = p.GetValueOrDefault("h", 0);
        double k = p.GetValueOrDefault("k", 0);
        double a = p.GetValueOrDefault("a", 2);
        double b = p.GetValueOrDefault("b", 1);
        if (a <= 0 || b <= 0) return new ConicResult { Success = false, Error = "Semi-axes a and b must be positive." };

        // (x-h)²/a² - (y-k)²/b² = 1  → horizontal hyperbola
        double c = Math.Sqrt(a * a + b * b);
        const int n = 300;
        var rightBranch = new List<GraphPoint>(n + 1);
        var leftBranch = new List<GraphPoint>(n + 1);

        // Parametric: x = h ± a*cosh(t), y = k + b*sinh(t)
        double tMax = 3.0; // covers a good visual range
        double dt = 2 * tMax / n;

        for (double t = -tMax; t <= tMax; t += dt)
        {
            double coshT = Math.Cosh(t);
            double sinhT = Math.Sinh(t);

            rightBranch.Add(new GraphPoint
            {
                X = Math.Round(h + a * coshT, 8),
                Y = Math.Round(k + b * sinhT, 8)
            });

            leftBranch.Add(new GraphPoint
            {
                X = Math.Round(h - a * coshT, 8),
                Y = Math.Round(k + b * sinhT, 8)
            });
        }

        return new ConicResult
        {
            Success = true,
            ConicType = "hyperbola",
            Equation = $"(x − {h})²/{a * a:G6} − (y − {k})²/{b * b:G6} = 1",
            Series = [rightBranch, leftBranch],
            Features =
            [
                new ConicFeature { Label = "Center", X = h, Y = k },
                new ConicFeature { Label = "Vertex 1", X = Math.Round(h + a, 6), Y = k },
                new ConicFeature { Label = "Vertex 2", X = Math.Round(h - a, 6), Y = k },
                new ConicFeature { Label = "Focus 1", X = Math.Round(h + c, 6), Y = k },
                new ConicFeature { Label = "Focus 2", X = Math.Round(h - c, 6), Y = k }
            ]
        };
    }
}
