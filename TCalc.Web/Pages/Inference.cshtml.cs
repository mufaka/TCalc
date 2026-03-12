using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class InferenceModel : PageModel
{
    private readonly IInferenceService _inference;

    public InferenceModel(IInferenceService inference) => _inference = inference;

    public void OnGet()
    {
    }

    // ── Bayesian Update ─────────────────────────────────────────────

    public IActionResult OnPostBayesianUpdate(
        [FromForm] double priorAlpha,
        [FromForm] double priorBeta,
        [FromForm] int successes,
        [FromForm] int trials)
    {
        var result = _inference.BayesianUpdate(priorAlpha, priorBeta, successes, trials);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.PriorAlpha,
            result.PriorBeta,
            result.PosteriorAlpha,
            result.PosteriorBeta,
            result.PosteriorMean,
            result.CredibleLower,
            result.CredibleUpper,
            priorCurve = result.PriorCurve.Select(p => new { p.X, p.Y }),
            likelihoodCurve = result.LikelihoodCurve.Select(p => new { p.X, p.Y }),
            posteriorCurve = result.PosteriorCurve.Select(p => new { p.X, p.Y }),
        });
    }

    // ── Z-Test ──────────────────────────────────────────────────────

    public IActionResult OnPostZTest(
        [FromForm] double sampleMean,
        [FromForm] double populationMean,
        [FromForm] double populationStdDev,
        [FromForm] int sampleSize,
        [FromForm] double alpha,
        [FromForm] string alternative)
    {
        var result = _inference.ZTest(sampleMean, populationMean, populationStdDev, sampleSize, alpha, alternative);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.TestStatistic,
            result.PValue,
            result.CriticalValue,
            result.RejectNull,
            result.Alternative,
            result.Alpha,
            distributionCurve = result.DistributionCurve.Select(p => new { p.X, p.Y }),
        });
    }

    // ── T-Test ──────────────────────────────────────────────────────

    public IActionResult OnPostTTest(
        [FromForm] double sampleMean,
        [FromForm] double populationMean,
        [FromForm] double sampleStdDev,
        [FromForm] int sampleSize,
        [FromForm] double alpha,
        [FromForm] string alternative)
    {
        var result = _inference.TTest(sampleMean, populationMean, sampleStdDev, sampleSize, alpha, alternative);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.TestStatistic,
            result.PValue,
            result.CriticalValue,
            result.RejectNull,
            result.Alternative,
            result.Alpha,
            result.DegreesOfFreedom,
            distributionCurve = result.DistributionCurve.Select(p => new { p.X, p.Y }),
        });
    }

    // ── Chi-Square Test ─────────────────────────────────────────────

    public IActionResult OnPostChiSquareTest(
        [FromForm] string observedJson,
        [FromForm] string expectedJson,
        [FromForm] double alpha)
    {
        double[]? observed;
        double[]? expected;
        try
        {
            observed = JsonSerializer.Deserialize<double[]>(observedJson);
            expected = JsonSerializer.Deserialize<double[]>(expectedJson);
        }
        catch
        {
            return new JsonResult(new { error = "Invalid data format." });
        }

        if (observed is null || expected is null)
            return new JsonResult(new { error = "Observed and expected arrays are required." });

        var result = _inference.ChiSquareTest(observed, expected, alpha);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.TestStatistic,
            result.PValue,
            result.CriticalValue,
            result.RejectNull,
            result.Alpha,
            result.DegreesOfFreedom,
        });
    }

    // ── MLE Normal ──────────────────────────────────────────────────

    public IActionResult OnPostMleNormal([FromForm] string dataJson)
    {
        double[]? data;
        try
        {
            data = JsonSerializer.Deserialize<double[]>(dataJson);
        }
        catch
        {
            return new JsonResult(new { error = "Invalid data format." });
        }

        if (data is null || data.Length == 0)
            return new JsonResult(new { error = "Data array is required." });

        var result = _inference.MleNormal(data);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.EstimatedMean,
            result.EstimatedStdDev,
            result.LogLikelihood,
            result.MuAxis,
            result.SigmaAxis,
            contourData = result.ContourData.Select(p => new { p.Mu, p.Sigma, p.LogLikelihood }),
        });
    }

    // ── MLE Binomial ────────────────────────────────────────────────

    public IActionResult OnPostMleBinomial(
        [FromForm] int trials,
        [FromForm] int successes)
    {
        var result = _inference.MleBinomial(trials, successes);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.Trials,
            result.Successes,
            result.EstimatedP,
            result.LogLikelihood,
            logLikelihoodCurve = result.LogLikelihoodCurve.Select(p => new { p.X, p.Y }),
        });
    }

    // ── Confidence Interval ─────────────────────────────────────────

    public IActionResult OnPostConfidenceInterval(
        [FromForm] double sampleMean,
        [FromForm] double stdDev,
        [FromForm] int sampleSize,
        [FromForm] double confidenceLevel,
        [FromForm] bool isPopulationStdDev)
    {
        var result = _inference.ConfidenceInterval(sampleMean, stdDev, sampleSize, confidenceLevel, isPopulationStdDev);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.SampleMean,
            result.StdDev,
            result.SampleSize,
            result.ConfidenceLevel,
            result.MarginOfError,
            result.LowerBound,
            result.UpperBound,
            result.CriticalValue,
            result.IntervalType,
        });
    }
}
