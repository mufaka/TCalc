# TCalc – Advanced Probability & Statistics: Visualization & Teaching Plan

> This document is the **secondary implementation plan** covering the interactive visualization and teaching tools
> for the advanced probability and statistics concepts described in `AdvancedMath.md`.
> It builds on the existing `ImplementationGuide.md` infrastructure (Alpine.js, HTMX, Chart.js, services layer).
>
> Mark each task **[x]** when it is complete.

---

## 0 — Page Restructure: Splitting the Probability Page

The current `Probability.cshtml` has grown to **7 tabs** (Basic, Conditional, Addition, Multiplication,
Experimental, Distribution Table, Joint Probability). Before adding advanced content, the page
should be decomposed into a focused set of pages.

### Target Page Architecture

| Page | Route | Focus | Source |
|------|-------|-------|--------|
| **Probability** | `/Probability` | Core probability rules & calculator | Existing — trimmed |
| **Distributions** | `/Distributions` | Distribution tables, named distributions, CLT, shape metrics | New + migrated |
| **Simulation** | `/Simulation` | Experimental probability, Monte Carlo, Markov Chains, Stochastic Processes | New + migrated |
| **Inference** | `/Inference` | Bayesian reasoning, hypothesis testing, MLE, confidence intervals | New |

### 0.1 — Refactor Probability Page

| # | Task | Status |
|---|------|--------|
| 0.1.1 | **Keep** on `Probability.cshtml`: Basic Concepts, Conditional, Addition Rule, Multiplication Rule, Quick Reference | [x] |
| 0.1.2 | **Move** Experimental Probability tab → `Simulation.cshtml` | [x] |
| 0.1.3 | **Move** Distribution Table tab → `Distributions.cshtml` | [x] |
| 0.1.4 | **Move** Joint Probability tab → `Distributions.cshtml` | [x] |
| 0.1.5 | Add cross-page navigation links at the bottom of each page ("Continue to Distributions →", etc.) | [x] |
| 0.1.6 | Update Quick Reference section on Probability page to only cover the rules that remain on-page | [x] |

### 0.2 — Navigation Updates

| # | Task | Status |
|---|------|--------|
| 0.2.1 | Add `Distributions`, `Simulation`, and `Inference` pills to `_Layout.cshtml` navbar — group these with Probability under a dropdown or secondary row to avoid navbar overflow | [x] |
| 0.2.2 | Add cards for the new pages on `Index.cshtml` home page | [x] |
| 0.2.3 | Consider a "Probability & Statistics" dropdown in the navbar containing: Probability, Distributions, Simulation, Inference | [x] |

---

## 1 — Distributions Page (`/Distributions`)

> Combines migrated tabs with new visualizations for named distributions, distribution shape metrics,
> the Central Limit Theorem, and Chebyshev's Inequality.

### Files to Create

- `Pages/Distributions.cshtml` + `Pages/Distributions.cshtml.cs`

### Tabs / Sections

1. **Distribution Table** — migrated from Probability page (editable table + bar chart + blocks)
2. **Joint Probability** — migrated from Probability page (table + heatmap + marginals chart)
3. **Named Distributions** — interactive Normal, Binomial, Poisson calculators with chart overlays
4. **Central Limit Theorem** — interactive CLT demonstration
5. **Shape Metrics** — Skewness & Kurtosis explorer
6. **Chebyshev's Inequality** — visual calculator

### 1A — Named Distributions (Interactive Overlay)

The existing Statistics page already has distribution calculators (§7A.13 in ImplementationGuide.md).
This section creates a more teaching-focused, side-by-side visualization.

| # | Task | Status |
|---|------|--------|
| 1A.1 | Create a **distribution selector** (Normal / Binomial / Poisson) with Alpine.js | [x] |
| 1A.2 | **Normal distribution panel**: sliders for μ (mean) and σ (std dev), live-updating PDF curve on Chart.js canvas. Shade region under curve for P(a ≤ X ≤ b) with configurable bounds. Display PDF value, CDF value, and z-score at cursor hover | [x] |
| 1A.3 | **Binomial distribution panel**: inputs for n (trials) and p (success probability), bar chart of PMF. Highlight bar at user-selected k, display P(X = k) and P(X ≤ k). Show E(X), Var(X), σ | [x] |
| 1A.4 | **Poisson distribution panel**: input for λ, bar chart of PMF. Same interaction pattern as binomial | [x] |
| 1A.5 | **Comparison mode**: overlay two distributions on the same chart (e.g., Binomial vs Normal approximation) with toggleable legends | [x] |
| 1A.6 | POST parameter changes via HTMX to `Distributions.cshtml.cs` handlers that call `IProbabilityService` — return JSON data points for client-side chart update | [x] |
| 1A.7 | Add teaching callouts: "The Normal curve is symmetric about μ", "68-95-99.7 rule", etc. — toggled by an "Explain" button | [x] |

### 1B — Central Limit Theorem Demonstration

> Visually demonstrate that the sampling distribution of the mean approaches Normal regardless of the population shape.

| # | Task | Status |
|---|------|--------|
| 1B.1 | Let user pick a **source population** shape: Uniform, Exponential, Bimodal, Skewed, Custom | [x] |
| 1B.2 | Render the population distribution as a histogram (Chart.js) | [x] |
| 1B.3 | Add a **sample size (n)** slider (1, 2, 5, 10, 30, 50, 100) and a **"Draw Samples"** button | [x] |
| 1B.4 | On each click, draw `n` values from the population, compute the sample mean, and add it to a running histogram of sample means | [x] |
| 1B.5 | Overlay the theoretical Normal curve N(μ, σ²/n) on the sample-means histogram | [x] |
| 1B.6 | Display running statistics: mean of means, std dev of means, number of samples drawn | [x] |
| 1B.7 | Add bulk-draw buttons (+100, +1000) for rapid convergence observation | [x] |
| 1B.8 | Add a **Reset** button to clear and start over | [x] |
| 1B.9 | Add teaching annotations: "Notice how even a skewed population produces bell-shaped means at n ≥ 30" | [x] |

### 1C — Skewness & Kurtosis Explorer

> Interactive tool to build intuition about distribution shape beyond mean and variance.

| # | Task | Status |
|---|------|--------|
| 1C.1 | Provide a set of **preset distributions** (Normal, Left-skewed, Right-skewed, Uniform, Leptokurtic, Platykurtic) with pre-computed data | [x] |
| 1C.2 | Render the distribution histogram plus a Normal overlay for comparison | [x] |
| 1C.3 | Compute and display: Skewness value with interpretation label ("Left-skewed" / "Symmetric" / "Right-skewed"), Kurtosis value with label ("Platykurtic" / "Mesokurtic" / "Leptokurtic") | [x] |
| 1C.4 | Add an **interactive data editor** where users can add/remove values and watch skewness/kurtosis update in real time | [x] |
| 1C.5 | Use `IStatisticsService.Compute()` via HTMX POST to calculate Skewness and Kurtosis server-side | [x] |
| 1C.6 | Visual annotation: vertical lines for mean, median, mode on the histogram to show how skewness shifts them | [x] |

### 1D — Chebyshev's Inequality Calculator

> For any distribution: P(|X − μ| ≥ kσ) ≤ 1/k²

| # | Task | Status |
|---|------|--------|
| 1D.1 | Input fields: Mean (μ), Std Dev (σ), number of standard deviations (k) | [x] |
| 1D.2 | Compute and display: minimum proportion within k std devs = 1 − 1/k², maximum proportion outside = 1/k² | [x] |
| 1D.3 | Comparison table: show Chebyshev bound vs actual Normal distribution proportion for k = 1, 1.5, 2, 2.5, 3 | [x] |
| 1D.4 | SVG or Chart.js visualization: a generic bell-ish shape with shaded regions at ±kσ, annotated with the guaranteed minimum percentage | [x] |
| 1D.5 | Teaching note: "Chebyshev works for ANY distribution — it's a worst-case guarantee" | [x] |

---

## 2 — Simulation Page (`/Simulation`)

> Hands-on experimentation: convergence, random processes, and computational estimation.

### Files to Create

- `Pages/Simulation.cshtml` + `Pages/Simulation.cshtml.cs`
- `Services/ISimulationService.cs` + `Services/SimulationService.cs`

### Tabs / Sections

1. **Experimental Probability** — migrated from Probability page (coin/die/custom trials + convergence chart)
2. **Monte Carlo** — estimate π, compute integrals, price options via random sampling
3. **Markov Chains** — build a transition matrix, step through states, visualize steady state
4. **Stochastic Processes** — Brownian Motion and Poisson Process real-time viewers

### 2.0 — Simulation Service (Back-End)

| # | Task | Status |
|---|------|--------|
| 2.0.1 | Create `ISimulationService` interface with methods: `MarkovStep`, `MarkovSteadyState`, `MonteCarloEstimatePi`, `MonteCarloIntegrate`, `BrownianMotionPath`, `PoissonProcessEvents` | [x] |
| 2.0.2 | Implement `SimulationService` | [x] |
| 2.0.3 | Register `ISimulationService` in `Program.cs` | [x] |
| 2.0.4 | Write unit tests for `SimulationService` in `TCalc.Tests` | [x] |

### 2A — Monte Carlo Simulations

| # | Task | Status |
|---|------|--------|
| 2A.1 | **Estimate π**: draw random (x, y) points in a unit square, check if they fall inside the quarter-circle. Render a scatter plot — hits in one colour, misses in another. Display running π estimate | [x] |
| 2A.2 | Add a progressive animation: points appear one-by-one (or in batches) and the estimate refines live | [x] |
| 2A.3 | **Integration estimator**: let user input a function f(x) and bounds [a, b]. Sample random x values, compute average f(x), multiply by (b−a). Compare to exact integral if known | [x] |
| 2A.4 | Scatter plot of sample points under/over the curve with Chart.js | [x] |
| 2A.5 | Controls: sample count (slider: 100 – 100,000), reset, auto-run toggle | [x] |
| 2A.6 | Teaching callout: "Monte Carlo trades exactness for generality — it works even when calculus doesn't" | [x] |

### 2B — Markov Chain Simulator

| # | Task | Status |
|---|------|--------|
| 2B.1 | **State editor**: let user define 2–6 states with labels (e.g., "Sunny", "Rainy", "Cloudy") | [x] |
| 2B.2 | **Transition matrix editor**: editable grid of probabilities (each row must sum to 1, with validation) | [x] |
| 2B.3 | Load presets: "Weather", "Random Walk", "Page Rank (3-page web)", "Gambler's Ruin" | [x] |
| 2B.4 | **State diagram**: SVG rendering of states as circles with directed arrows labeled by transition probabilities. Highlight the current state | [x] |
| 2B.5 | **Step controls**: "Next Step" button advances one transition; "Auto-run" steps on a timer; "Run 100 steps" for batch | [x] |
| 2B.6 | **History panel**: show the sequence of visited states and a frequency bar chart comparing visit proportions to steady-state distribution | [x] |
| 2B.7 | **Steady-state calculator**: compute πQ = πQ (left eigenvector) server-side via HTMX POST to `SimulationService.MarkovSteadyState()` and display the stationary distribution | [x] |
| 2B.8 | Teaching notes: "The Markov property: the future depends only on the present, not the past" | [x] |

### 2C — Stochastic Processes Viewer

| # | Task | Status |
|---|------|--------|
| 2C.1 | **Brownian Motion**: generate a random walk path with configurable drift (μ) and volatility (σ). Render as a line chart updating in real-time (requestAnimationFrame or setInterval). Add sliders for μ, σ, and time-step Δt | [x] |
| 2C.2 | Overlay multiple sample paths simultaneously to visualize the "cone of uncertainty" | [x] |
| 2C.3 | **Poisson Process**: generate event arrival times with configurable rate λ. Render as a step-function (cumulative events over time) and as ticks on a timeline | [x] |
| 2C.4 | Display inter-arrival time histogram alongside — show it converges to Exponential(λ) | [x] |
| 2C.5 | Teaching notes: "Brownian Motion models stock prices; Poisson Process models call arrivals, earthquakes, website hits" | [x] |

---

## 3 — Inference Page (`/Inference`)

> Statistical reasoning: updating beliefs, testing hypotheses, estimating parameters.

### Files to Create

- `Pages/Inference.cshtml` + `Pages/Inference.cshtml.cs`
- `Services/IInferenceService.cs` + `Services/InferenceService.cs`

### Tabs / Sections

1. **Bayesian Updater** — interactive prior → posterior visualization
2. **Hypothesis Testing** — step-by-step test calculator with visual p-value
3. **Maximum Likelihood Estimation** — interactive MLE for common distributions
4. **Confidence Intervals** — calculator and visualization
5. **Non-Parametric Methods** — sign test, rank-sum overview with worked examples

### 3.0 — Inference Service (Back-End)

| # | Task | Status |
|---|------|--------|
| 3.0.1 | Create `IInferenceService` interface with methods: `BayesianUpdate`, `ZTest`, `TTest`, `ChiSquareTest`, `MleNormal`, `MleBinomial`, `ConfidenceInterval` | [x] |
| 3.0.2 | Implement `InferenceService` | [x] |
| 3.0.3 | Register `IInferenceService` in `Program.cs` | [x] |
| 3.0.4 | Write unit tests for `InferenceService` in `TCalc.Tests` | [x] |

### 3A — Bayesian Updater

> The crown jewel of the teaching tools. Shows how beliefs evolve as evidence accumulates.

| # | Task | Status |
|---|------|--------|
| 3A.1 | **Prior selector**: choose a prior distribution shape — Uniform (no prior belief), Beta(α, β) with custom parameters, or preset priors ("Skeptic", "Believer", "Neutral") | [x] |
| 3A.2 | **Evidence input**: enter number of successes (k) and trials (n) from an observed experiment | [x] |
| 3A.3 | **Posterior visualization**: render Prior curve (dashed), Likelihood curve (dotted), and Posterior curve (solid) on the same Chart.js canvas over θ ∈ [0, 1] | [x] |
| 3A.4 | Display computed posterior parameters: Beta(α + k, β + n − k), posterior mean, 95% credible interval | [x] |
| 3A.5 | **Sequential update mode**: allow multiple rounds of evidence. After each round, the previous posterior becomes the new prior. Show animation of curve shifting | [x] |
| 3A.6 | **Reset** button to return to the original prior | [x] |
| 3A.7 | Teaching callout: "Posterior ∝ Likelihood × Prior — more data makes the prior less influential" | [x] |

### 3B — Hypothesis Testing Calculator

| # | Task | Status |
|---|------|--------|
| 3B.1 | **Test type selector**: One-sample z-test, One-sample t-test, Two-sample t-test, Chi-square goodness-of-fit | [x] |
| 3B.2 | **Input forms** (shown/hidden by test type): sample mean, population mean, std dev, sample size, significance level α, alternative hypothesis (two-tailed / left / right) | [x] |
| 3B.3 | POST inputs via HTMX to `InferenceService` handlers; return test statistic, p-value, critical value, decision | [x] |
| 3B.4 | **Visual p-value**: render the sampling distribution curve (Normal or t), shade the rejection region(s), and mark the test statistic with a vertical line. Label the shaded area with the p-value | [x] |
| 3B.5 | **Step-by-step explanation panel**: show each step of the test procedure: (1) State hypotheses, (2) Choose α, (3) Compute test statistic, (4) Find p-value, (5) Decision — toggled by an "Explain Steps" button | [x] |
| 3B.6 | **Type I / Type II error visualizer**: overlay two distributions (H₀ and Hₐ), shade α region and β region, show how changing α or n affects power | [x] |
| 3B.7 | Teaching notes: "A small p-value means the observed data is unlikely under H₀ — it does NOT prove Hₐ is true" | [x] |

### 3C — Maximum Likelihood Estimation (MLE)

| # | Task | Status |
|---|------|--------|
| 3C.1 | **Data input**: paste or enter a set of observed values | [x] |
| 3C.2 | **Distribution selector**: Normal (estimate μ, σ), Exponential (estimate λ), Binomial (estimate p given n) | [x] |
| 3C.3 | **Likelihood surface plot**: for 1-parameter models, render log-likelihood as a curve over the parameter space, with a marker at the MLE. For 2-parameter (Normal), render a contour heatmap of log-likelihood over (μ, σ) | [x] |
| 3C.4 | Compute MLE estimates server-side and return via HTMX | [x] |
| 3C.5 | **Overlay fitted distribution** on a histogram of the raw data to visually assess fit | [x] |
| 3C.6 | Teaching note: "MLE finds the parameter values that make your observed data most probable" | [x] |

### 3D — Confidence Intervals

| # | Task | Status |
|---|------|--------|
| 3D.1 | Input: sample mean, sample std dev (or known population σ), sample size, confidence level (90%, 95%, 99%, custom) | [x] |
| 3D.2 | Compute interval server-side (z-interval or t-interval as appropriate) | [x] |
| 3D.3 | **Number line visualization**: draw the interval as a segment on a horizontal axis, with the point estimate at center and margin of error annotated | [x] |
| 3D.4 | **Repeated sampling demo**: generate 100 random confidence intervals from a simulated population. Color-code intervals that capture the true mean (≈95% of them should). This builds the correct frequentist interpretation | [x] |
| 3D.5 | Teaching note: "A 95% CI does NOT mean 95% probability the parameter is in this interval — it means if we repeated the study many times, ~95% of intervals would capture it" | [x] |

### 3E — Non-Parametric Methods (Overview)

| # | Task | Status |
|---|------|--------|
| 3E.1 | **When to use**: decision flowchart SVG — "Is your data normally distributed?" → No → "Use non-parametric methods" | [x] |
| 3E.2 | **Sign Test calculator**: input paired differences, compute test statistic, p-value | [x] |
| 3E.3 | **Wilcoxon Rank-Sum overview**: step-by-step worked example with two small samples, ranking visualization | [x] |
| 3E.4 | Comparison table: parametric test vs non-parametric equivalent (t-test → Wilcoxon, ANOVA → Kruskal-Wallis, etc.) | [x] |

---

## 4 — Information Theory: Entropy Calculator

> Can be added as an additional tab on the **Distributions** page, since entropy is a property of a distribution.

| # | Task | Status |
|---|------|--------|
| 4.1 | Add an **Entropy** tab to the Distributions page | [x] |
| 4.2 | Input: a discrete probability distribution (reuse the Distribution Table editor or enter directly) | [x] |
| 4.3 | Compute Shannon Entropy: H(X) = −Σ pᵢ log₂(pᵢ). Also display in nats (ln) and bans (log₁₀) | [x] |
| 4.4 | **Entropy bar**: horizontal gauge from 0 (certain) to log₂(n) (maximum entropy = uniform distribution). Mark the computed value | [x] |
| 4.5 | **Comparison panel**: show entropy of the user's distribution vs. the uniform distribution with the same number of outcomes | [x] |
| 4.6 | **Interactive slider experiment**: start with a 2-outcome distribution. Slide P(A) from 0 → 1 and watch entropy rise to max at 0.5 then fall — renders as a live curve | [x] |
| 4.7 | Teaching note: "Entropy measures surprise — a fair coin (H=1 bit) carries maximum information per flip" | [x] |

---

## 5 — Predictive & Multivariate Analysis Hooks

> These topics are computationally heavy. The plan provides meaningful interactive entry points
> without requiring a full ML framework. PCA and ARIMA can link to the existing Statistics/Regression services.

### 5A — Time Series (ARIMA Lite)

> Add as a tab on the **Statistics** page or as a standalone section on the Simulation page.

| # | Task | Status |
|---|------|--------|
| 5A.1 | **Data input**: paste time-indexed data or load a preset (e.g., "Airline Passengers", "Monthly Temperatures") | [x] |
| 5A.2 | **Decomposition chart**: render the original series, trend component, seasonal component, and residual using Chart.js (simple moving-average decomposition) | [x] |
| 5A.3 | **Autocorrelation plot (ACF)**: compute and display lagged correlations as a bar chart with significance bands | [x] |
| 5A.4 | **Forecast slider**: let user choose forecast horizon (1–12 periods). Use a simple exponential smoothing or linear trend extrapolation and display predicted values with a shaded confidence cone | [x] |
| 5A.5 | Create `Services/ITimeSeriesService.cs` with `Decompose`, `Autocorrelation`, `ForecastSimple` methods | [x] |
| 5A.6 | Implement `Services/TimeSeriesService.cs` | [x] |
| 5A.7 | Register in `Program.cs` and add unit tests | [x] |

### 5B — PCA Visualizer (2-D)

> A simplified 2-D principal component demonstration to build geometric intuition.

| # | Task | Status |
|---|------|--------|
| 5B.1 | **Scatter plot input**: let user enter or paste a set of (x, y) data points, or load presets ("Correlated", "Uncorrelated", "Elliptical") | [x] |
| 5B.2 | Render the scatter plot on a Chart.js canvas with equal-aspect axes | [x] |
| 5B.3 | Compute mean-centred data, covariance matrix, eigenvalues/eigenvectors server-side | [x] |
| 5B.4 | **Overlay principal component axes** as arrows from the centroid, scaled by eigenvalue magnitude | [x] |
| 5B.5 | **Projection toggle**: button to project all points onto PC1 — animate the dots sliding to the first principal axis to show dimensionality reduction in action | [x] |
| 5B.6 | Display explained variance ratio for PC1 and PC2 | [x] |
| 5B.7 | Teaching note: "PCA finds the directions of maximum spread (variance) — PC1 captures the most information" | [x] |

### 5C — Regression Enhancement (Multiple & Logistic)

> The existing `IRegressionService` covers Linear and Polynomial. Extend for Multiple Linear and Logistic.

| # | Task | Status |
|---|------|--------|
| 5C.1 | Extend `IRegressionService` with `MultipleLinearRegression(double[][] X, double[] y)` returning coefficients, R², adjusted R² | [x] |
| 5C.2 | Extend `IRegressionService` with `LogisticRegression(double[] x, bool[] y)` returning β₀, β₁, predicted probabilities, classification accuracy | [x] |
| 5C.3 | Add a **Multiple Regression** tab/section on the Statistics page: spreadsheet input for multiple predictors, results table with coefficients and p-values | [x] |
| 5C.4 | Add a **Logistic Regression** tab/section: scatter plot of 0/1 outcomes, overlay the sigmoid curve, interactive threshold slider to see how classification changes | [x] |
| 5C.5 | Unit tests for the new regression methods | [x] |

---

## 6 — Shared UI Patterns & Components

> Reusable patterns to keep the new pages consistent with existing TCalc style.

| # | Task | Status |
|---|------|--------|
| 6.1 | Create a shared **teaching callout** partial or CSS component: collapsible "💡 Learn" panel with a consistent look (`prob-teach-card` class) | [x] |
| 6.2 | Create a **formula display** component: renders LaTeX-style formulas cleanly in HTML (use KaTeX CDN for proper math rendering) | [x] |
| 6.3 | Add KaTeX CSS + JS to `_Layout.cshtml` (lightweight, renders server-authored LaTeX strings on the client) | [x] |
| 6.4 | Create a reusable **"data entry grid"** Alpine.js component (used by CLT, MLE, PCA, Non-Parametric) — extract from the existing Statistics page pattern | [x] |
| 6.5 | Add CSS classes for the new page sections matching existing `prob-*` naming convention: `sim-*` for simulation, `inf-*` for inference, `dist-*` for distributions | [x] |
| 6.6 | Ensure all Chart.js instances respect dark/light theme via CSS custom properties | [x] |

---

## 7 — Testing

| # | Task | Status |
|---|------|--------|
| 7.1 | Unit tests for `SimulationService` — Markov steady-state convergence, Monte Carlo π estimation within tolerance, Brownian Motion path generation | [x] |
| 7.2 | Unit tests for `InferenceService` — z-test / t-test against known values, Bayesian Beta posterior parameters, MLE estimates for Normal data, confidence interval coverage | [x] |
| 7.3 | Unit tests for `TimeSeriesService` — decomposition of known series, autocorrelation of white noise ≈ 0 | [x] |
| 7.4 | Unit tests for extended `RegressionService` — multiple regression coefficients, logistic regression sigmoid correctness | [x] |
| 7.5 | Integration tests for new page handlers (Distributions, Simulation, Inference) using `WebApplicationFactory<Program>` | [x] |

---

## Recommended Implementation Order

```
Phase A  — Page Restructure     → §0               (split Probability, update nav)
Phase B  — Distributions Page   → §1A, §1B         (named distributions, CLT demo)
Phase C  — Distribution Extras  → §1C, §1D, §4     (Skewness/Kurtosis, Chebyshev, Entropy)
Phase D  — Simulation Service   → §2.0             (back-end for Monte Carlo, Markov)
Phase E  — Simulation Page      → §2A, §2B         (Monte Carlo, Markov Chains)
Phase F  — Stochastic Viewer    → §2C              (Brownian Motion, Poisson Process)
Phase G  — Inference Service    → §3.0             (back-end for Bayesian, hypothesis, MLE)
Phase H  — Inference Page       → §3A, §3B         (Bayesian Updater, Hypothesis Testing)
Phase I  — Inference Extras     → §3C, §3D, §3E    (MLE, Confidence Intervals, Non-Parametric)
Phase J  — Predictive Analysis  → §5A, §5B, §5C    (Time Series, PCA, Regression extensions)
Phase K  — Shared UI & Polish   → §6               (KaTeX, teaching callouts, components)
Phase L  — Testing              → §7               (full test coverage for new services)
```

---

## File / Folder Structure (Additions)

```
TCalc.Web/
├── Pages/
│   ├── Probability.cshtml / .cs        ← trimmed (4 tabs: Basic, Conditional, Addition, Multiplication)
│   ├── Distributions.cshtml / .cs      ← NEW (Distribution Table, Joint, Named, CLT, Shape, Chebyshev, Entropy)
│   ├── Simulation.cshtml / .cs         ← NEW (Experimental, Monte Carlo, Markov, Stochastic)
│   └── Inference.cshtml / .cs          ← NEW (Bayesian, Hypothesis, MLE, Confidence, Non-Parametric)
├── Services/
│   ├── ISimulationService.cs           ← NEW
│   ├── SimulationService.cs            ← NEW
│   ├── IInferenceService.cs            ← NEW
│   ├── InferenceService.cs             ← NEW
│   ├── ITimeSeriesService.cs           ← NEW
│   └── TimeSeriesService.cs            ← NEW
│   ├── IRegressionService.cs           ← EXTENDED (Multiple + Logistic)
│   └── RegressionService.cs            ← EXTENDED
└── Specifications/
    ├── AdvancedMath.md                 ← concept reference (existing)
    └── AdvancedImplementationGuide.md  ← THIS FILE

TCalc.Tests/
├── SimulationServiceTests.cs           ← NEW
├── InferenceServiceTests.cs            ← NEW
└── TimeSeriesServiceTests.cs           ← NEW
```

---

## Concept-to-Page Mapping (from AdvancedMath.md)

| AdvancedMath.md Concept | Target Page | Section |
|-------------------------|-------------|---------|
| Bayesian Statistics | Inference | §3A — Bayesian Updater |
| Markov Chains | Simulation | §2B — Markov Chain Simulator |
| Stochastic Processes (Brownian, Poisson) | Simulation | §2C — Stochastic Processes Viewer |
| Information Theory (Entropy) | Distributions | §4 — Entropy Calculator |
| Maximum Likelihood Estimation | Inference | §3C — MLE |
| Central Limit Theorem | Distributions | §1B — CLT Demonstration |
| Hypothesis Testing & P-Values | Inference | §3B — Hypothesis Testing Calculator |
| Non-Parametric Statistics | Inference | §3E — Non-Parametric Methods |
| Time Series Analysis (ARIMA) | Statistics* | §5A — Time Series |
| Principal Component Analysis | Statistics* | §5B — PCA Visualizer |
| Multiple & Logistic Regression | Statistics* | §5C — Regression Enhancement |
| Monte Carlo Simulations | Simulation | §2A — Monte Carlo Simulations |
| Skewness & Kurtosis | Distributions | §1C — Shape Metrics |
| Chebyshev's Inequality | Distributions | §1D — Chebyshev Calculator |

*\* Could also be placed on a new "Analysis" page if the Statistics page becomes too busy.*

---

## Design Principles

1. **Calculator-first**: Every concept gets at least one interactive input → computed output loop. Users should be able to plug in their own numbers and see results immediately.
2. **Visualization as primary teaching tool**: Every computation is paired with a chart, diagram, or animation. The visual should update reactively as inputs change.
3. **Progressive disclosure**: Start with the calculator; hide the "how it works" explanation behind a "💡 Learn" toggle so power users aren't slowed down and learners aren't overwhelmed.
4. **Consistent tech stack**: Alpine.js for client state, HTMX for server round-trips, Chart.js for charts, inline SVG for diagrams. No new JS frameworks.
5. **Server-side math**: All non-trivial computation happens in C# services (testable, accurate). The client only handles UI state and chart rendering.
6. **Preset data**: Every tool includes at least 2–3 preset/example datasets so users can see the tool in action before entering their own data.

---

*Last updated: 2025-07-13*
