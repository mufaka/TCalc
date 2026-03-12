namespace TCalc.Web.Services;

/// <summary>
/// Implements Monte Carlo estimation, Markov chain simulation,
/// and stochastic process generation.
/// </summary>
public sealed class SimulationService : ISimulationService
{
    private readonly ILogger<SimulationService> _logger;

    public SimulationService(ILogger<SimulationService> logger) => _logger = logger;

    // ── Monte Carlo Estimate π ──────────────────────────────────────

    public MonteCarloEstimatePiResult MonteCarloEstimatePi(int sampleCount, int? seed = null)
    {
        if (sampleCount < 1)
            return MonteCarloEstimatePiResult.Fail("Sample count must be at least 1.");
        if (sampleCount > 1_000_000)
            return MonteCarloEstimatePiResult.Fail("Sample count must not exceed 1,000,000.");

        _logger.LogDebug("MonteCarloEstimatePi: {Count} samples", sampleCount);

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        int inside = 0;

        // Limit the number of points we return for rendering
        int maxDisplay = Math.Min(sampleCount, 5000);
        var points = new List<MonteCarloPoint>(maxDisplay);
        int displayEvery = Math.Max(1, sampleCount / maxDisplay);

        for (int i = 0; i < sampleCount; i++)
        {
            double x = rng.NextDouble();
            double y = rng.NextDouble();
            bool isInside = x * x + y * y <= 1.0;
            if (isInside) inside++;

            if (points.Count < maxDisplay && (i % displayEvery == 0 || i == sampleCount - 1))
                points.Add(new MonteCarloPoint { X = x, Y = y, Inside = isInside });
        }

        double piEstimate = 4.0 * inside / sampleCount;

        return new MonteCarloEstimatePiResult
        {
            Success = true,
            PiEstimate = piEstimate,
            TotalPoints = sampleCount,
            InsideCircle = inside,
            Points = [.. points],
        };
    }

    // ── Monte Carlo Integration ─────────────────────────────────────

    /// <summary>
    /// Known functions for Monte Carlo integration.
    /// </summary>
    private static readonly Dictionary<string, (Func<double, double> F, Func<double, double, double?> Exact, string Display)> KnownFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x^2"] = (x => x * x, (a, b) => (b * b * b - a * a * a) / 3.0, "x²"),
        ["x^3"] = (x => x * x * x, (a, b) => (Math.Pow(b, 4) - Math.Pow(a, 4)) / 4.0, "x³"),
        ["sin(x)"] = (Math.Sin, (a, b) => -Math.Cos(b) + Math.Cos(a), "sin(x)"),
        ["cos(x)"] = (Math.Cos, (a, b) => Math.Sin(b) - Math.Sin(a), "cos(x)"),
        ["e^x"] = (Math.Exp, (a, b) => Math.Exp(b) - Math.Exp(a), "eˣ"),
        ["e^(-x^2)"] = (x => Math.Exp(-x * x), (_, _) => null, "e^(−x²)"),   // no closed-form
        ["sqrt(x)"] = (x => x >= 0 ? Math.Sqrt(x) : 0, (a, b) => a >= 0 ? (2.0 / 3.0) * (Math.Pow(b, 1.5) - Math.Pow(a, 1.5)) : null, "√x"),
        ["1/x"] = (x => x != 0 ? 1.0 / x : 0, (a, b) => a > 0 && b > 0 ? Math.Log(b) - Math.Log(a) : null, "1/x"),
        ["x"] = (x => x, (a, b) => (b * b - a * a) / 2.0, "x"),
    };

    public MonteCarloIntegrateResult MonteCarloIntegrate(string functionName, double a, double b, int sampleCount, int? seed = null)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            return MonteCarloIntegrateResult.Fail("Function name is required.");
        if (sampleCount < 1)
            return MonteCarloIntegrateResult.Fail("Sample count must be at least 1.");
        if (sampleCount > 1_000_000)
            return MonteCarloIntegrateResult.Fail("Sample count must not exceed 1,000,000.");
        if (b <= a)
            return MonteCarloIntegrateResult.Fail("Upper bound (b) must be greater than lower bound (a).");

        if (!KnownFunctions.TryGetValue(functionName.Trim(), out var entry))
            return MonteCarloIntegrateResult.Fail($"Unknown function: '{functionName}'. Supported: {string.Join(", ", KnownFunctions.Keys)}");

        _logger.LogDebug("MonteCarloIntegrate: {Func} over [{A}, {B}], {Count} samples", functionName, a, b, sampleCount);

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        double sum = 0;

        // For scatter plot, find max |f(x)| to generate random y in bounding box
        double maxY = 0;
        int probeCount = 200;
        for (int i = 0; i <= probeCount; i++)
        {
            double px = a + (b - a) * i / probeCount;
            double fy = entry.F(px);
            if (Math.Abs(fy) > maxY) maxY = Math.Abs(fy);
        }
        if (maxY == 0) maxY = 1;
        maxY *= 1.1; // padding

        int maxDisplay = Math.Min(sampleCount, 3000);
        int displayEvery = Math.Max(1, sampleCount / maxDisplay);
        var points = new List<IntegrationPoint>(maxDisplay);

        for (int i = 0; i < sampleCount; i++)
        {
            double x = a + (b - a) * rng.NextDouble();
            double fx = entry.F(x);
            sum += fx;

            if (i % displayEvery == 0 || i == sampleCount - 1)
            {
                double sy = rng.NextDouble() * maxY;
                points.Add(new IntegrationPoint
                {
                    X = x,
                    FunctionY = fx,
                    SampleY = sy,
                    UnderCurve = sy <= fx && fx >= 0,
                });
            }
        }

        double estimate = (b - a) * sum / sampleCount;
        double? exact = entry.Exact(a, b);

        return new MonteCarloIntegrateResult
        {
            Success = true,
            FunctionName = entry.Display,
            A = a,
            B = b,
            Estimate = estimate,
            ExactValue = exact,
            SampleCount = sampleCount,
            Points = [.. points],
        };
    }

    // ── Markov Step ─────────────────────────────────────────────────

    public MarkovStepResult MarkovStep(double[][] transitionMatrix, int currentState, int? seed = null)
    {
        string? validation = ValidateTransitionMatrix(transitionMatrix);
        if (validation is not null)
            return MarkovStepResult.Fail(validation);

        int n = transitionMatrix.Length;
        if (currentState < 0 || currentState >= n)
            return MarkovStepResult.Fail($"Current state {currentState} is out of range [0, {n - 1}].");

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        double r = rng.NextDouble();
        double cumulative = 0;
        int nextState = n - 1; // fallback to last state

        for (int j = 0; j < n; j++)
        {
            cumulative += transitionMatrix[currentState][j];
            if (r < cumulative)
            {
                nextState = j;
                break;
            }
        }

        return new MarkovStepResult
        {
            Success = true,
            PreviousState = currentState,
            NextState = nextState,
        };
    }

    // ── Markov Steady State ─────────────────────────────────────────

    public MarkovSteadyStateResult MarkovSteadyState(double[][] transitionMatrix)
    {
        string? validation = ValidateTransitionMatrix(transitionMatrix);
        if (validation is not null)
            return MarkovSteadyStateResult.Fail(validation);

        int n = transitionMatrix.Length;
        _logger.LogDebug("MarkovSteadyState: {N}x{N} matrix", n);

        // Power iteration: start with uniform distribution, repeatedly multiply by P
        double[] pi = new double[n];
        for (int i = 0; i < n; i++) pi[i] = 1.0 / n;

        const int maxIterations = 10_000;
        const double tolerance = 1e-10;
        bool converged = false;
        int iterations = 0;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            double[] next = new double[n];
            for (int j = 0; j < n; j++)
            {
                double s = 0;
                for (int i = 0; i < n; i++)
                    s += pi[i] * transitionMatrix[i][j];
                next[j] = s;
            }

            // Check convergence
            double maxDiff = 0;
            for (int i = 0; i < n; i++)
            {
                double diff = Math.Abs(next[i] - pi[i]);
                if (diff > maxDiff) maxDiff = diff;
            }

            pi = next;
            iterations = iter + 1;

            if (maxDiff < tolerance)
            {
                converged = true;
                break;
            }
        }

        return new MarkovSteadyStateResult
        {
            Success = true,
            Distribution = pi,
            Iterations = iterations,
            Converged = converged,
        };
    }

    // ── Brownian Motion ─────────────────────────────────────────────

    public BrownianMotionResult BrownianMotionPath(double drift, double volatility, double dt, int steps, int? seed = null)
    {
        if (dt <= 0)
            return BrownianMotionResult.Fail("Time step (Δt) must be positive.");
        if (steps < 1)
            return BrownianMotionResult.Fail("Number of steps must be at least 1.");
        if (steps > 10_000)
            return BrownianMotionResult.Fail("Number of steps must not exceed 10,000.");
        if (volatility < 0)
            return BrownianMotionResult.Fail("Volatility (σ) must be non-negative.");

        _logger.LogDebug("BrownianMotionPath: drift={Drift}, vol={Vol}, dt={Dt}, steps={Steps}", drift, volatility, dt, steps);

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var path = new BrownianMotionPoint[steps + 1];
        double value = 0;
        path[0] = new BrownianMotionPoint { Time = 0, Value = 0 };

        double sqrtDt = Math.Sqrt(dt);

        for (int i = 1; i <= steps; i++)
        {
            // Box-Muller transform for standard normal
            double z = BoxMullerNormal(rng);
            value += drift * dt + volatility * sqrtDt * z;
            path[i] = new BrownianMotionPoint { Time = i * dt, Value = value };
        }

        return new BrownianMotionResult
        {
            Success = true,
            Drift = drift,
            Volatility = volatility,
            Dt = dt,
            Path = path,
        };
    }

    // ── Poisson Process ─────────────────────────────────────────────

    public PoissonProcessResult PoissonProcessEvents(double lambda, double duration, int? seed = null)
    {
        if (lambda <= 0)
            return PoissonProcessResult.Fail("Rate (λ) must be positive.");
        if (duration <= 0)
            return PoissonProcessResult.Fail("Duration must be positive.");
        if (duration > 10_000)
            return PoissonProcessResult.Fail("Duration must not exceed 10,000.");

        _logger.LogDebug("PoissonProcessEvents: lambda={Lambda}, duration={Duration}", lambda, duration);

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var arrivals = new List<double>();
        var interArrivals = new List<double>();
        double time = 0;

        while (true)
        {
            // Inter-arrival time ~ Exponential(λ)
            double u = rng.NextDouble();
            if (u == 0) u = double.Epsilon; // avoid log(0)
            double interArrival = -Math.Log(u) / lambda;
            time += interArrival;

            if (time > duration) break;

            arrivals.Add(time);
            interArrivals.Add(interArrival);

            // Safety: limit the number of events
            if (arrivals.Count >= 100_000) break;
        }

        return new PoissonProcessResult
        {
            Success = true,
            Lambda = lambda,
            Duration = duration,
            EventCount = arrivals.Count,
            ArrivalTimes = [.. arrivals],
            InterArrivalTimes = [.. interArrivals],
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static string? ValidateTransitionMatrix(double[][] matrix)
    {
        if (matrix is null || matrix.Length == 0)
            return "Transition matrix is empty.";

        int n = matrix.Length;
        if (n > 20)
            return "Transition matrix must not exceed 20×20.";

        for (int i = 0; i < n; i++)
        {
            if (matrix[i] is null || matrix[i].Length != n)
                return $"Row {i} must have exactly {n} columns.";

            double rowSum = 0;
            for (int j = 0; j < n; j++)
            {
                if (matrix[i][j] < 0)
                    return $"Transition probability at [{i}][{j}] must be non-negative.";
                rowSum += matrix[i][j];
            }

            if (Math.Abs(rowSum - 1.0) > 0.01)
                return $"Row {i} probabilities must sum to 1 (got {rowSum:F4}).";
        }

        return null;
    }

    private static double BoxMullerNormal(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble(); // avoid log(0)
        double u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
