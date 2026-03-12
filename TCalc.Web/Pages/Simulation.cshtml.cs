using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class SimulationModel : PageModel
{
    private readonly ISimulationService _sim;

    public SimulationModel(ISimulationService sim) => _sim = sim;

    public void OnGet()
    {
    }

    // ── Monte Carlo π ───────────────────────────────────────────────

    public IActionResult OnPostEstimatePi([FromForm] int sampleCount)
    {
        var result = _sim.MonteCarloEstimatePi(sampleCount);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.PiEstimate,
            result.TotalPoints,
            result.InsideCircle,
            points = result.Points.Select(p => new { p.X, p.Y, p.Inside }),
        });
    }

    // ── Monte Carlo Integration ─────────────────────────────────────

    public IActionResult OnPostIntegrate(
        [FromForm] string functionName,
        [FromForm] double a,
        [FromForm] double b,
        [FromForm] int sampleCount)
    {
        var result = _sim.MonteCarloIntegrate(functionName, a, b, sampleCount);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.FunctionName,
            result.A,
            result.B,
            result.Estimate,
            result.ExactValue,
            result.SampleCount,
            points = result.Points.Select(p => new { p.X, p.FunctionY, p.SampleY, p.UnderCurve }),
        });
    }

    // ── Markov Steady State ─────────────────────────────────────────

    public IActionResult OnPostSteadyState([FromForm] string matrixJson)
    {
        if (string.IsNullOrWhiteSpace(matrixJson))
            return new JsonResult(new { error = "Transition matrix is required." });

        double[][]? matrix;
        try
        {
            matrix = JsonSerializer.Deserialize<double[][]>(matrixJson);
        }
        catch
        {
            return new JsonResult(new { error = "Invalid matrix format." });
        }

        if (matrix is null)
            return new JsonResult(new { error = "Transition matrix is required." });

        var result = _sim.MarkovSteadyState(matrix);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.Distribution,
            result.Iterations,
            result.Converged,
        });
    }

    // ── Markov Step ─────────────────────────────────────────────────

    public IActionResult OnPostMarkovStep(
        [FromForm] string matrixJson,
        [FromForm] int currentState)
    {
        if (string.IsNullOrWhiteSpace(matrixJson))
            return new JsonResult(new { error = "Transition matrix is required." });

        double[][]? matrix;
        try
        {
            matrix = JsonSerializer.Deserialize<double[][]>(matrixJson);
        }
        catch
        {
            return new JsonResult(new { error = "Invalid matrix format." });
        }

        if (matrix is null)
            return new JsonResult(new { error = "Transition matrix is required." });

        var result = _sim.MarkovStep(matrix, currentState);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.PreviousState,
            result.NextState,
        });
    }

    // ── Brownian Motion Path ────────────────────────────────────────

    public IActionResult OnPostBrownianMotion(
        [FromForm] double drift,
        [FromForm] double volatility,
        [FromForm] double dt,
        [FromForm] int steps)
    {
        var result = _sim.BrownianMotionPath(drift, volatility, dt, steps);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.Drift,
            result.Volatility,
            result.Dt,
            path = result.Path.Select(p => new { p.Time, p.Value }),
        });
    }

    // ── Poisson Process Events ──────────────────────────────────────

    public IActionResult OnPostPoissonProcess(
        [FromForm] double lambda,
        [FromForm] double duration)
    {
        var result = _sim.PoissonProcessEvents(lambda, duration);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.Lambda,
            result.Duration,
            result.EventCount,
            result.ArrivalTimes,
            result.InterArrivalTimes,
        });
    }
}
