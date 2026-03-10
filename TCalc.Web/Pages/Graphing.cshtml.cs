using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class GraphingModel : PageModel
{
    private readonly IGraphingService _graphing;

    public GraphingModel(IGraphingService graphing)
    {
        _graphing = graphing;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPostPlot([FromForm] string equations, [FromForm] double xMin, [FromForm] double xMax)
    {
        if (string.IsNullOrWhiteSpace(equations))
            return new JsonResult(new { error = "No equations provided." });

        double step = (xMax - xMin) / 500.0;
        if (step <= 0) step = 0.1;

        var lines = equations.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var datasets = new List<object>();

        foreach (var line in lines)
        {
            // Check if the expression is an inequality
            var ineqResult = _graphing.EvaluateInequality(line, xMin, xMax, step, -10, 10);
            if (ineqResult.Success && ineqResult.IsInequality)
            {
                datasets.Add(new
                {
                    expression = line,
                    isInequality = true,
                    @operator = ineqResult.Operator,
                    fillDirection = ineqResult.FillDirection,
                    boundaryInclusive = ineqResult.BoundaryInclusive,
                    points = ineqResult.BoundaryPoints.Select(p => new { p.X, p.Y })
                });
                continue;
            }

            var result = _graphing.GeneratePoints(line, xMin, xMax, step);
            if (result.Success)
            {
                datasets.Add(new
                {
                    expression = result.Expression,
                    points = result.Points.Select(p => new { p.X, p.Y }),
                    diagnostics = result.ErrorCount > 0 ? new
                    {
                        totalPoints = result.TotalPoints,
                        errorCount = result.ErrorCount,
                        sampleError = result.SampleError,
                        sampleSubstitution = result.SampleSubstitution
                    } : null
                });
            }
            else
            {
                datasets.Add(new
                {
                    expression = result.Expression,
                    error = result.Error,
                    diagnostics = new
                    {
                        totalPoints = result.TotalPoints,
                        errorCount = result.ErrorCount,
                        sampleError = result.SampleError,
                        sampleSubstitution = result.SampleSubstitution
                    }
                });
            }
        }

        return new JsonResult(datasets);
    }

    public IActionResult OnPostTransform(
        [FromForm] string baseExpression,
        [FromForm] string transformType,
        [FromForm] double paramH,
        [FromForm] double paramK,
        [FromForm] double paramScale,
        [FromForm] double xMin,
        [FromForm] double xMax)
    {
        if (string.IsNullOrWhiteSpace(baseExpression))
            return new JsonResult(new { error = "Base expression is required." });

        double step = (xMax - xMin) / 500.0;
        if (step <= 0) step = 0.1;

        // Generate original curve
        var original = _graphing.GeneratePoints(baseExpression, xMin, xMax, step);
        if (!original.Success)
            return new JsonResult(new { error = original.Error });

        // Build transformed expression based on type
        string transformedExpr = BuildTransformedExpression(baseExpression, transformType, paramH, paramK, paramScale);
        var transformed = _graphing.GeneratePoints(transformedExpr, xMin, xMax, step);

        return new JsonResult(new
        {
            original = new { expression = baseExpression, points = original.Points.Select(p => new { p.X, p.Y }) },
            transformed = new { expression = transformedExpr, points = transformed.Points.Select(p => new { p.X, p.Y }) },
            transformDescription = DescribeTransform(transformType, paramH, paramK, paramScale)
        });
    }

    public IActionResult OnPostConic(
        [FromForm] string conicType,
        [FromForm] double h,
        [FromForm] double k,
        [FromForm] double paramA,
        [FromForm] double paramB)
    {
        var parameters = new Dictionary<string, double>
        {
            ["h"] = h,
            ["k"] = k,
        };

        // Map generic paramA/paramB to the shape-specific parameters
        switch (conicType?.ToLowerInvariant())
        {
            case "circle":
                parameters["r"] = paramA;
                break;
            case "ellipse":
            case "hyperbola":
                parameters["a"] = paramA;
                parameters["b"] = paramB;
                break;
            case "parabola":
                parameters["a"] = paramA;
                break;
        }

        var result = _graphing.GenerateConicPoints(conicType ?? "", parameters);
        if (!result.Success)
            return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            conicType = result.ConicType,
            equation = result.Equation,
            series = result.Series.Select(s => s.Select(p => new { p.X, p.Y })),
            features = result.Features.Select(f => new { f.Label, f.X, f.Y })
        });
    }

    private static string BuildTransformedExpression(string baseExpr, string transformType, double h, double k, double scale)
    {
        return transformType?.ToLowerInvariant() switch
        {
            "translate" => $"({baseExpr.Replace("x", $"(x-({h}))", StringComparison.OrdinalIgnoreCase)})+({k})",
            "reflect-x" => $"-({baseExpr})",
            "reflect-y" => baseExpr.Replace("x", "(-x)", StringComparison.OrdinalIgnoreCase),
            "stretch-v" => $"({scale})*({baseExpr})",
            "stretch-h" => baseExpr.Replace("x", $"(x/({scale}))", StringComparison.OrdinalIgnoreCase),
            _ => baseExpr
        };
    }

    private static string DescribeTransform(string transformType, double h, double k, double scale)
    {
        return transformType?.ToLowerInvariant() switch
        {
            "translate" => $"Translated by ({h}, {k})",
            "reflect-x" => "Reflected across x-axis",
            "reflect-y" => "Reflected across y-axis",
            "stretch-v" => $"Vertical stretch by factor {scale}",
            "stretch-h" => $"Horizontal stretch by factor {scale}",
            _ => "No transform"
        };
    }
}
