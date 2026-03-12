using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class DistributionsModel : PageModel
{
    private readonly IProbabilityService _probability;
    private readonly IStatisticsService _stats;

    public DistributionsModel(IProbabilityService probability, IStatisticsService stats)
    {
        _probability = probability;
        _stats = stats;
    }

    public void OnGet()
    {
    }

    // ── Named Distribution handlers ────────────────────────────────

    public IActionResult OnPostProbability(
        [FromForm] string distribution,
        [FromForm] double? mean,
        [FromForm] double? stdDev,
        [FromForm] int? n,
        [FromForm] double? p,
        [FromForm] double? lambda,
        [FromForm] double? x,
        [FromForm] int? k)
    {
        return distribution?.ToLowerInvariant() switch
        {
            "normal" => ComputeNormal(mean, stdDev, x),
            "binomial" => ComputeBinomial(n, p, k),
            "poisson" => ComputePoisson(lambda, k),
            _ => new JsonResult(new { error = "Unknown distribution type." }),
        };
    }

    private IActionResult ComputeNormal(double? mean, double? stdDev, double? x)
    {
        if (!mean.HasValue) return new JsonResult(new { error = "Mean (μ) is required." });
        if (!stdDev.HasValue) return new JsonResult(new { error = "Standard deviation (σ) is required." });

        var result = _probability.Normal(mean.Value, stdDev.Value, x);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            type = "normal",
            result.Mean,
            result.StdDev,
            pdfAtX = result.PdfAtX,
            cdfAtX = result.CdfAtX,
            curve = result.PdfCurve.Select(pt => new { pt.X, pt.Y }),
            queriedX = x,
        });
    }

    private IActionResult ComputeBinomial(int? n, double? p, int? k)
    {
        if (!n.HasValue) return new JsonResult(new { error = "Number of trials (n) is required." });
        if (!p.HasValue) return new JsonResult(new { error = "Probability of success (p) is required." });

        var result = _probability.Binomial(n.Value, p.Value, k);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            type = "binomial",
            result.N,
            result.P,
            result.ExpectedValue,
            result.Variance,
            result.StdDev,
            pmfAtK = result.PmfAtK,
            cdfAtK = result.CdfAtK,
            points = result.PmfPoints.Select(pt => new { pt.X, pt.Y }),
            queriedK = k,
        });
    }

    private IActionResult ComputePoisson(double? lambda, int? k)
    {
        if (!lambda.HasValue) return new JsonResult(new { error = "Rate parameter (λ) is required." });

        var result = _probability.Poisson(lambda.Value, k);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            type = "poisson",
            result.Lambda,
            result.ExpectedValue,
            result.Variance,
            result.StdDev,
            pmfAtK = result.PmfAtK,
            cdfAtK = result.CdfAtK,
            points = result.PmfPoints.Select(pt => new { pt.X, pt.Y }),
            queriedK = k,
        });
    }

    // ── Shape Metrics handler ──────────────────────────────────────

    public IActionResult OnPostShapeMetrics([FromForm] string data)
    {
        double[]? values = ParseData(data);
        if (values is null || values.Length < 3)
            return new JsonResult(new { error = "Enter at least 3 numeric values." });

        var stats = _stats.Compute(values);
        if (!stats.Success)
            return new JsonResult(new { error = stats.Error });

        // Build histogram bins (Sturges' rule)
        int binCount = Math.Max(1, (int)Math.Ceiling(Math.Log2(values.Length) + 1));
        double binWidth = stats.Range / binCount;
        if (binWidth <= 0) { binCount = 1; binWidth = 1; }

        var bins = new List<object>();
        for (int i = 0; i < binCount; i++)
        {
            double lo = stats.Min + i * binWidth;
            double hi = lo + binWidth;
            int count = values.Count(v => v >= lo && (i == binCount - 1 ? v <= hi : v < hi));
            bins.Add(new { label = $"{lo:G4}–{hi:G4}", midpoint = lo + binWidth / 2, count });
        }

        return new JsonResult(new
        {
            stats.Mean,
            stats.Median,
            mode = stats.Mode.Length > 0 ? stats.Mode[0] : stats.Mean,
            stats.StdDev,
            stats.Skewness,
            stats.Kurtosis,
            stats.Count,
            histogram = bins,
        });
    }

    // ── helpers ─────────────────────────────────────────────────────

    private static double[]? ParseData(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var values = new List<double>();
        foreach (string token in raw.Split([',', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (double.TryParse(token, CultureInfo.InvariantCulture, out double v))
                values.Add(v);
        }
        return values.Count > 0 ? [.. values] : null;
    }
}
