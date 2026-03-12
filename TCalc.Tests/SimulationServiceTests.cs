using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class SimulationServiceTests
{
    private readonly SimulationService _service = new(NullLogger<SimulationService>.Instance);

    // ════════════════════════════════════════════════════════════════
    //  Monte Carlo Estimate π
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MonteCarloEstimatePi_ReturnsEstimateWithinTolerance()
    {
        var result = _service.MonteCarloEstimatePi(100_000, seed: 42);

        Assert.True(result.Success);
        Assert.InRange(result.PiEstimate, 3.0, 3.3); // generous tolerance
        Assert.Equal(100_000, result.TotalPoints);
        Assert.True(result.InsideCircle > 0);
        Assert.True(result.InsideCircle < result.TotalPoints);
    }

    [Fact]
    public void MonteCarloEstimatePi_WithSeed_IsDeterministic()
    {
        var r1 = _service.MonteCarloEstimatePi(10_000, seed: 123);
        var r2 = _service.MonteCarloEstimatePi(10_000, seed: 123);

        Assert.Equal(r1.PiEstimate, r2.PiEstimate);
        Assert.Equal(r1.InsideCircle, r2.InsideCircle);
    }

    [Fact]
    public void MonteCarloEstimatePi_InvalidCount_ReturnsError()
    {
        var result = _service.MonteCarloEstimatePi(0);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void MonteCarloEstimatePi_LimitsDisplayPoints()
    {
        var result = _service.MonteCarloEstimatePi(50_000, seed: 1);
        Assert.True(result.Success);
        Assert.True(result.Points.Length <= 5000);
    }

    [Fact]
    public void MonteCarloEstimatePi_AllPointsInsideUnitSquare()
    {
        var result = _service.MonteCarloEstimatePi(1000, seed: 7);
        Assert.True(result.Success);
        foreach (var pt in result.Points)
        {
            Assert.InRange(pt.X, 0, 1);
            Assert.InRange(pt.Y, 0, 1);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Monte Carlo Integration
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("x^2", 0, 1, 0.3333)]   // ∫₀¹ x² dx = 1/3
    [InlineData("x", 0, 2, 2.0)]         // ∫₀² x dx = 2
    [InlineData("sin(x)", 0, 3.14159, 2.0)] // ∫₀^π sin(x) dx = 2
    public void MonteCarloIntegrate_ApproximatesKnownIntegrals(string func, double a, double b, double expected)
    {
        var result = _service.MonteCarloIntegrate(func, a, b, 200_000, seed: 42);

        Assert.True(result.Success);
        Assert.InRange(result.Estimate, expected - 0.1, expected + 0.1);
    }

    [Fact]
    public void MonteCarloIntegrate_ReturnsExactValue_WhenKnown()
    {
        var result = _service.MonteCarloIntegrate("x^2", 0, 1, 1000, seed: 1);

        Assert.True(result.Success);
        Assert.NotNull(result.ExactValue);
        Assert.Equal(1.0 / 3.0, result.ExactValue!.Value, 10);
    }

    [Fact]
    public void MonteCarloIntegrate_UnknownFunction_ReturnsError()
    {
        var result = _service.MonteCarloIntegrate("magic(x)", 0, 1, 100);
        Assert.False(result.Success);
        Assert.Contains("Unknown function", result.Error);
    }

    [Fact]
    public void MonteCarloIntegrate_InvalidBounds_ReturnsError()
    {
        var result = _service.MonteCarloIntegrate("x^2", 5, 3, 100);
        Assert.False(result.Success);
    }

    // ════════════════════════════════════════════════════════════════
    //  Markov Step
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkovStep_ReturnsValidNextState()
    {
        // Simple 2-state chain: always go to state 1
        double[][] matrix = [[0, 1], [0, 1]];

        var result = _service.MarkovStep(matrix, 0, seed: 1);

        Assert.True(result.Success);
        Assert.Equal(0, result.PreviousState);
        Assert.Equal(1, result.NextState);
    }

    [Fact]
    public void MarkovStep_WithSeed_IsDeterministic()
    {
        double[][] matrix = [[0.5, 0.5], [0.3, 0.7]];

        var r1 = _service.MarkovStep(matrix, 0, seed: 42);
        var r2 = _service.MarkovStep(matrix, 0, seed: 42);

        Assert.Equal(r1.NextState, r2.NextState);
    }

    [Fact]
    public void MarkovStep_InvalidState_ReturnsError()
    {
        double[][] matrix = [[1.0]];
        var result = _service.MarkovStep(matrix, 5);
        Assert.False(result.Success);
    }

    [Fact]
    public void MarkovStep_InvalidMatrix_RowNotSumOne_ReturnsError()
    {
        double[][] matrix = [[0.3, 0.3], [0.5, 0.5]];
        var result = _service.MarkovStep(matrix, 0);
        Assert.False(result.Success);
        Assert.Contains("sum to 1", result.Error);
    }

    [Fact]
    public void MarkovStep_NegativeProbability_ReturnsError()
    {
        double[][] matrix = [[-0.1, 1.1], [0.5, 0.5]];
        var result = _service.MarkovStep(matrix, 0);
        Assert.False(result.Success);
        Assert.Contains("non-negative", result.Error);
    }

    // ════════════════════════════════════════════════════════════════
    //  Markov Steady State
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkovSteadyState_UniformStaysUniform()
    {
        // Doubly stochastic matrix → uniform steady state
        double[][] matrix = [[0.5, 0.5], [0.5, 0.5]];

        var result = _service.MarkovSteadyState(matrix);

        Assert.True(result.Success);
        Assert.True(result.Converged);
        Assert.Equal(2, result.Distribution.Length);
        Assert.Equal(0.5, result.Distribution[0], 4);
        Assert.Equal(0.5, result.Distribution[1], 4);
    }

    [Fact]
    public void MarkovSteadyState_KnownSteadyState()
    {
        // Weather model: Sunny(0) ↔ Rainy(1)
        // P(S→S) = 0.8, P(S→R) = 0.2, P(R→S) = 0.4, P(R→R) = 0.6
        // Steady state: π_S = 0.4/(0.2+0.4) = 2/3, π_R = 1/3
        double[][] matrix = [[0.8, 0.2], [0.4, 0.6]];

        var result = _service.MarkovSteadyState(matrix);

        Assert.True(result.Success);
        Assert.True(result.Converged);
        Assert.Equal(2.0 / 3.0, result.Distribution[0], 3);
        Assert.Equal(1.0 / 3.0, result.Distribution[1], 3);
    }

    [Fact]
    public void MarkovSteadyState_ThreeStateChain()
    {
        // 3-state chain with known steady state
        double[][] matrix = [
            [0.1, 0.6, 0.3],
            [0.4, 0.2, 0.4],
            [0.3, 0.3, 0.4],
        ];

        var result = _service.MarkovSteadyState(matrix);

        Assert.True(result.Success);
        Assert.True(result.Converged);
        Assert.Equal(3, result.Distribution.Length);

        // Sum should be 1
        double sum = result.Distribution.Sum();
        Assert.Equal(1.0, sum, 4);

        // All positive
        foreach (var p in result.Distribution)
            Assert.True(p > 0);
    }

    [Fact]
    public void MarkovSteadyState_EmptyMatrix_ReturnsError()
    {
        var result = _service.MarkovSteadyState([]);
        Assert.False(result.Success);
    }

    // ════════════════════════════════════════════════════════════════
    //  Brownian Motion
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void BrownianMotionPath_GeneratesCorrectNumberOfPoints()
    {
        var result = _service.BrownianMotionPath(drift: 0, volatility: 1, dt: 0.01, steps: 100, seed: 42);

        Assert.True(result.Success);
        Assert.Equal(101, result.Path.Length); // steps + 1 (includes t=0)
    }

    [Fact]
    public void BrownianMotionPath_StartsAtZero()
    {
        var result = _service.BrownianMotionPath(drift: 0.5, volatility: 1, dt: 0.1, steps: 50, seed: 1);

        Assert.True(result.Success);
        Assert.Equal(0.0, result.Path[0].Time);
        Assert.Equal(0.0, result.Path[0].Value);
    }

    [Fact]
    public void BrownianMotionPath_TimeIncrementsCorrectly()
    {
        double dt = 0.05;
        var result = _service.BrownianMotionPath(drift: 0, volatility: 1, dt: dt, steps: 10, seed: 1);

        Assert.True(result.Success);
        for (int i = 0; i <= 10; i++)
        {
            Assert.Equal(i * dt, result.Path[i].Time, 10);
        }
    }

    [Fact]
    public void BrownianMotionPath_ZeroVolatility_PureDrift()
    {
        double drift = 0.5;
        double dt = 0.1;
        var result = _service.BrownianMotionPath(drift: drift, volatility: 0, dt: dt, steps: 10, seed: 1);

        Assert.True(result.Success);
        for (int i = 0; i <= 10; i++)
        {
            Assert.Equal(drift * dt * i, result.Path[i].Value, 10);
        }
    }

    [Fact]
    public void BrownianMotionPath_WithSeed_IsDeterministic()
    {
        var r1 = _service.BrownianMotionPath(0, 1, 0.01, 100, seed: 77);
        var r2 = _service.BrownianMotionPath(0, 1, 0.01, 100, seed: 77);

        Assert.Equal(r1.Path.Length, r2.Path.Length);
        for (int i = 0; i < r1.Path.Length; i++)
            Assert.Equal(r1.Path[i].Value, r2.Path[i].Value);
    }

    [Fact]
    public void BrownianMotionPath_InvalidSteps_ReturnsError()
    {
        var result = _service.BrownianMotionPath(0, 1, 0.01, 0);
        Assert.False(result.Success);
    }

    [Fact]
    public void BrownianMotionPath_NegativeVolatility_ReturnsError()
    {
        var result = _service.BrownianMotionPath(0, -1, 0.01, 10);
        Assert.False(result.Success);
    }

    // ════════════════════════════════════════════════════════════════
    //  Poisson Process
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void PoissonProcessEvents_GeneratesEvents()
    {
        var result = _service.PoissonProcessEvents(lambda: 5, duration: 10, seed: 42);

        Assert.True(result.Success);
        Assert.True(result.EventCount > 0);
        Assert.Equal(result.EventCount, result.ArrivalTimes.Length);
        Assert.Equal(result.EventCount, result.InterArrivalTimes.Length);
    }

    [Fact]
    public void PoissonProcessEvents_ArrivalTimesAreOrdered()
    {
        var result = _service.PoissonProcessEvents(lambda: 3, duration: 20, seed: 1);

        Assert.True(result.Success);
        for (int i = 1; i < result.ArrivalTimes.Length; i++)
        {
            Assert.True(result.ArrivalTimes[i] > result.ArrivalTimes[i - 1]);
        }
    }

    [Fact]
    public void PoissonProcessEvents_AllTimesWithinDuration()
    {
        double duration = 10;
        var result = _service.PoissonProcessEvents(lambda: 5, duration: duration, seed: 99);

        Assert.True(result.Success);
        foreach (var t in result.ArrivalTimes)
        {
            Assert.True(t > 0);
            Assert.True(t <= duration);
        }
    }

    [Fact]
    public void PoissonProcessEvents_MeanInterArrivalApproximatesInverseLambda()
    {
        double lambda = 10;
        var result = _service.PoissonProcessEvents(lambda: lambda, duration: 1000, seed: 42);

        Assert.True(result.Success);
        Assert.True(result.InterArrivalTimes.Length > 100);

        double meanIAT = result.InterArrivalTimes.Average();
        double expected = 1.0 / lambda;

        // Within 10% tolerance
        Assert.InRange(meanIAT, expected * 0.9, expected * 1.1);
    }

    [Fact]
    public void PoissonProcessEvents_MeanEventCountApproximatesLambdaTimesDuration()
    {
        double lambda = 5;
        double duration = 100;
        var result = _service.PoissonProcessEvents(lambda: lambda, duration: duration, seed: 42);

        Assert.True(result.Success);
        double expectedCount = lambda * duration;

        // Within 10% tolerance
        Assert.InRange(result.EventCount, expectedCount * 0.85, expectedCount * 1.15);
    }

    [Fact]
    public void PoissonProcessEvents_InvalidLambda_ReturnsError()
    {
        var result = _service.PoissonProcessEvents(lambda: 0, duration: 10);
        Assert.False(result.Success);
    }

    [Fact]
    public void PoissonProcessEvents_InvalidDuration_ReturnsError()
    {
        var result = _service.PoissonProcessEvents(lambda: 5, duration: -1);
        Assert.False(result.Success);
    }

    [Fact]
    public void PoissonProcessEvents_WithSeed_IsDeterministic()
    {
        var r1 = _service.PoissonProcessEvents(lambda: 3, duration: 10, seed: 55);
        var r2 = _service.PoissonProcessEvents(lambda: 3, duration: 10, seed: 55);

        Assert.Equal(r1.EventCount, r2.EventCount);
        Assert.Equal(r1.ArrivalTimes, r2.ArrivalTimes);
    }
}
