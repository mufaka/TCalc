namespace TCalc.Web.Services;

/// <summary>
/// Provides methods for Monte Carlo estimation, Markov chain simulation,
/// and stochastic process generation.
/// </summary>
public interface ISimulationService
{
    /// <summary>
    /// Estimates π by sampling random points in the unit square and checking
    /// whether they fall inside the quarter-circle.
    /// </summary>
    MonteCarloEstimatePiResult MonteCarloEstimatePi(int sampleCount, int? seed = null);

    /// <summary>
    /// Estimates the integral of f(x) over [a, b] via Monte Carlo sampling.
    /// The function is identified by name (e.g. "x^2", "sin(x)", "e^(-x^2)").
    /// </summary>
    MonteCarloIntegrateResult MonteCarloIntegrate(string functionName, double a, double b, int sampleCount, int? seed = null);

    /// <summary>
    /// Advances a Markov chain by one step from the given state using the transition matrix.
    /// Returns the next state index.
    /// </summary>
    MarkovStepResult MarkovStep(double[][] transitionMatrix, int currentState, int? seed = null);

    /// <summary>
    /// Computes the steady-state (stationary) distribution of a Markov chain
    /// by solving πP = π via iterative power method.
    /// </summary>
    MarkovSteadyStateResult MarkovSteadyState(double[][] transitionMatrix);

    /// <summary>
    /// Generates a Brownian Motion sample path.
    /// </summary>
    BrownianMotionResult BrownianMotionPath(double drift, double volatility, double dt, int steps, int? seed = null);

    /// <summary>
    /// Generates Poisson Process event arrival times.
    /// </summary>
    PoissonProcessResult PoissonProcessEvents(double lambda, double duration, int? seed = null);
}

// ── Monte Carlo Estimate π ──────────────────────────────────────────────

public sealed class MonteCarloEstimatePiResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double PiEstimate { get; init; }
    public int TotalPoints { get; init; }
    public int InsideCircle { get; init; }

    /// <summary>
    /// Sample points for rendering: each has X, Y, and whether it is Inside the quarter-circle.
    /// Limited to a displayable subset when sample count is large.
    /// </summary>
    public MonteCarloPoint[] Points { get; init; } = [];

    public static MonteCarloEstimatePiResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public sealed class MonteCarloPoint
{
    public double X { get; init; }
    public double Y { get; init; }
    public bool Inside { get; init; }
}

// ── Monte Carlo Integration ─────────────────────────────────────────────

public sealed class MonteCarloIntegrateResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public string FunctionName { get; init; } = "";
    public double A { get; init; }
    public double B { get; init; }
    public double Estimate { get; init; }
    public double? ExactValue { get; init; }
    public int SampleCount { get; init; }

    /// <summary>Points sampled: X, FunctionY (actual f(x)), SampleY (random y for scatter), UnderCurve flag.</summary>
    public IntegrationPoint[] Points { get; init; } = [];

    public static MonteCarloIntegrateResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public sealed class IntegrationPoint
{
    public double X { get; init; }
    public double FunctionY { get; init; }
    public double SampleY { get; init; }
    public bool UnderCurve { get; init; }
}

// ── Markov Step ─────────────────────────────────────────────────────────

public sealed class MarkovStepResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public int PreviousState { get; init; }
    public int NextState { get; init; }

    public static MarkovStepResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Markov Steady State ─────────────────────────────────────────────────

public sealed class MarkovSteadyStateResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>The stationary distribution vector π where πP = π.</summary>
    public double[] Distribution { get; init; } = [];
    public int Iterations { get; init; }
    public bool Converged { get; init; }

    public static MarkovSteadyStateResult Fail(string error) =>
        new() { Success = false, Error = error };
}

// ── Brownian Motion ─────────────────────────────────────────────────────

public sealed class BrownianMotionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double Drift { get; init; }
    public double Volatility { get; init; }
    public double Dt { get; init; }

    /// <summary>Path points: Time and Value at each step.</summary>
    public BrownianMotionPoint[] Path { get; init; } = [];

    public static BrownianMotionResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public sealed class BrownianMotionPoint
{
    public double Time { get; init; }
    public double Value { get; init; }
}

// ── Poisson Process ─────────────────────────────────────────────────────

public sealed class PoissonProcessResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public double Lambda { get; init; }
    public double Duration { get; init; }
    public int EventCount { get; init; }

    /// <summary>Arrival times of each event.</summary>
    public double[] ArrivalTimes { get; init; } = [];

    /// <summary>Inter-arrival times (exponentially distributed).</summary>
    public double[] InterArrivalTimes { get; init; } = [];

    public static PoissonProcessResult Fail(string error) =>
        new() { Success = false, Error = error };
}
