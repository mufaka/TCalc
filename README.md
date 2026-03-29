# TCalc

A web-based graphing calculator and mathematical analysis application built with ASP.NET Core. TCalc provides a comprehensive suite of tools for students, educators, and professionals — from basic arithmetic to advanced statistical inference.

## Features

- **Standard Calculator** – Basic arithmetic, percentages, square roots, and sign toggling
- **Scientific Calculator** – Trigonometric functions (sin, cos, tan and inverses), logarithms, constants (π, e), factorials, and degree/radian modes
- **Graphing Calculator** – Plot mathematical functions with zoom/pan; supports inequalities, function transformations, and conic sections (circles, ellipses, parabolas, hyperbolas)
- **Geometry Calculator** – Area, perimeter, volume, and surface area for common shapes with visual representations
- **Statistics** – Descriptive statistics (mean, median, mode, variance, std dev, quartiles, skewness, kurtosis), data visualization (histograms, box plots, scatter plots), and CSV import
- **Regression Analysis** – Linear, polynomial, multiple linear, and logistic regression with R² diagnostics
- **Probability & Distributions** – Normal, Binomial, and Poisson distributions; conditional/joint/total probability; Central Limit Theorem demonstrations; Chebyshev's Inequality
- **Simulation** – Monte Carlo simulations and Markov chains
- **Inference** – Bayesian inference, hypothesis testing (with p-values), confidence intervals, and Maximum Likelihood Estimation (MLE)
- **Time Series** – ARIMA-style forecasting and trend decomposition
- **User Dashboard** – Save data sets and graphing workspaces; export data as CSV (requires account)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# on .NET 10 |
| Web Framework | ASP.NET Core 10 (Razor Pages) |
| Database | SQLite via Entity Framework Core 10 |
| Authentication | ASP.NET Core Identity |
| Frontend | Alpine.js, HTMX, Chart.js, jQuery |
| CSV Processing | CsvHelper |
| Testing | xUnit, Microsoft.AspNetCore.Mvc.Testing |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- No other external dependencies required

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd TCalc
```

### 2. Apply database migrations

```bash
dotnet ef database update --project TCalc.Web
```

This creates a `tcalc.db` SQLite file in the project directory.

### 3. Run the application

```bash
dotnet run --project TCalc.Web/TCalc.Web.csproj
```

The application starts on **http://localhost:5075** and opens automatically in your browser.

### Build for release

```bash
dotnet build --configuration Release
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

The test suite covers:

| Test File | Coverage |
|-----------|----------|
| `CalculatorEngineTests.cs` | Expression parsing and evaluation (40+ cases) |
| `StatisticsServiceTests.cs` | Descriptive statistics calculations |
| `GraphingServiceTests.cs` | Function point generation and inequality handling |
| `GeometryServiceTests.cs` | Geometric calculations |
| `RegressionServiceTests.cs` | Linear, polynomial, multiple, and logistic regression |
| `ProbabilityServiceTests.cs` | Distribution calculations |
| `SimulationServiceTests.cs` | Monte Carlo and Markov chain simulations |
| `InferenceServiceTests.cs` | Bayesian and hypothesis testing |
| `TimeSeriesServiceTests.cs` | Time series forecasting |
| `PageIntegrationTests.cs` | Razor Pages integration tests |

## Project Structure

```
TCalc/
├── TCalc.slnx                  # Solution file
├── TCalc.Web/                  # Main web application
│   ├── Pages/                  # Razor Pages (UI + handlers)
│   ├── Services/               # Business logic (calculator, stats, graphing, etc.)
│   ├── Models/                 # EF Core entity models
│   ├── Data/                   # ApplicationDbContext
│   ├── Areas/Identity/         # Authentication pages
│   ├── Migrations/             # EF Core database migrations
│   ├── wwwroot/                # Static assets (CSS, JS, third-party libs)
│   ├── appsettings.json        # Application configuration
│   └── Program.cs              # Application entry point
└── TCalc.Tests/                # xUnit test project
```

## Configuration

Key settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tcalc.db"
  },
  "CsvUpload": {
    "MaxRowCount": 10000
  }
}
```

- The SQLite database file (`tcalc.db`) is created automatically on first run.
- CSV uploads are capped at 10,000 rows by default.
- No environment variables are required.

## Architecture

TCalc uses a clean service-layer architecture:

- **Razor Pages** handle HTTP requests and return HTML fragments (via HTMX) or JSON.
- **Service interfaces** (`ICalculatorEngine`, `IStatisticsService`, etc.) encapsulate all business logic and are registered as dependency-injected singletons.
- **Entity Framework Core** with ASP.NET Core Identity manages user accounts and saved data.

The `CalculatorEngine` implements a recursive-descent parser that evaluates mathematical expressions with correct operator precedence, parentheses, and scientific functions.
