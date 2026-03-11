# TCalc – Implementation Guide

> This document tracks every milestone needed to turn the scaffolded ASP.NET Core Razor Pages project into the full TCalc graphing-calculator application described in `Overview.md`.
> Mark each task **[x]** when it is complete.

---

## 0 — Project Foundations

| # | Task | Status |
|---|------|--------|
| 0.1 | Add client-side library dependencies (Alpine.js, HTMX, Chart.js / D3.js) via `libman.json` or CDN references | [x] |
| 0.2 | Remove jQuery (replaced by Alpine.js + HTMX for interactivity) or keep only if a dependency requires it | [x] |
| 0.3 | Create a `libman.json` manifest listing all third-party client libraries and their target paths under `wwwroot/lib/` | [ ] |
| 0.4 | Update `_Layout.cshtml` – add Alpine.js (`<script defer src="...">`), HTMX (`<script src="...">`), and Chart.js/D3.js references | [x] |
| 0.5 | Add a favicon and branding (update `<title>`, navbar brand, and footer text to say **TCalc**) | [x] |
| 0.6 | Establish a shared CSS custom-property theme in `wwwroot/css/site.css` (colours, spacing, typography tokens) | [x] |
| 0.7 | Add NuGet packages: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `CsvHelper` | [x] |
| 0.8 | Create `Data/ApplicationDbContext.cs` inheriting `IdentityDbContext` and configure SQLite connection string in `appsettings.json` | [x] |
| 0.9 | Register `ApplicationDbContext` and ASP.NET Core Identity services in `Program.cs` | [x] |
| 0.10 | Add initial EF Core migration and apply it (`dotnet ef migrations add InitialCreate`, `dotnet ef database update`) | [x] |
| 0.11 | Scaffold or create Identity Razor Pages under `Pages/Account/` (Register, Login, Logout, AccessDenied) or use `AddDefaultUI()` | [x] |
| 0.12 | Add login/register/logout links to `_Layout.cshtml` navbar (show user name when authenticated) | [x] |
| 0.13 | Add `UseAuthentication()` middleware in `Program.cs` before `UseAuthorization()` | [x] |

---

## 1 — Responsive Layout & Navigation

| # | Task | Status |
|---|------|--------|
| 1.1 | Redesign `_Layout.cshtml` with a responsive sidebar/tab navigation for calculator modes (Standard · Scientific · Graphing · Geometry · Statistics) | [x] |
| 1.2 | Add Bootstrap responsive breakpoints so the layout collapses to a bottom-tab bar on small screens | [ ] |
| 1.3 | Add a `<meta name="theme-color">` tag and manifest for PWA-style mobile experience (optional stretch) | [x] |

---

## 2 — Dark Mode

| # | Task | Status |
|---|------|--------|
| 2.1 | Define light and dark CSS custom-property palettes in `site.css` using `[data-theme="dark"]` selector | [x] |
| 2.2 | Add a theme-toggle button in the navbar | [x] |
| 2.3 | Wire toggle with Alpine.js (`x-data`, `x-on:click`) to flip `data-theme` on `<html>` and persist choice in `localStorage` | [x] |
| 2.4 | On page load, restore the saved theme before first paint (inline `<script>` in `<head>`) to avoid flash-of-wrong-theme | [x] |

---

## 3 — Calculation Engine (Back-End)

> All maths logic lives in a dedicated service layer so Razor Pages stay thin.

| # | Task | Status |
|---|------|--------|
| 3.1 | Create `Services/ICalculatorEngine.cs` interface with methods: `Evaluate(string expression)`, `EvaluateFunction(string name, double[] args)` | [x] |
| 3.2 | Implement `Services/CalculatorEngine.cs` – parse & evaluate arithmetic expressions (order of operations, parentheses) | [x] |
| 3.3 | Add scientific function support: `sin`, `cos`, `tan`, `log`, `ln`, `sqrt`, `pow`, `exp`, `abs`, `factorial`, `%` | [x] |
| 3.4 | Add expression validation & meaningful error messages | [x] |
| 3.5 | Register the engine in `Program.cs` via `builder.Services.AddSingleton<ICalculatorEngine, CalculatorEngine>()` | [x] |
| 3.6 | Write unit tests (`TCalc.Tests` project) for the engine covering edge cases (division by zero, invalid input, operator precedence) | [x] |

---

## 4 — Standard Calculator Page

| # | Task | Status |
|---|------|--------|
| 4.1 | Create `Pages/Standard.cshtml` + `Standard.cshtml.cs` Razor Page | [x] |
| 4.2 | Build the calculator UI: display area, digit buttons (0-9), operator buttons (+, −, ×, ÷), clear, backspace, equals | [x] |
| 4.3 | Add buttons for `√`, `x²`, `%`, `±` | [x] |
| 4.4 | Use Alpine.js `x-data` to manage the display expression and live input state entirely on the client | [x] |
| 4.5 | On **=** press, POST the expression to a Razor Page handler (`OnPostCalculate`) via HTMX (`hx-post`, `hx-target`) and display the result without a full page reload | [x] |
| 4.6 | Support keyboard input (number keys, operators, Enter = evaluate, Escape = clear) | [x] |
| 4.7 | Add calculation history panel (stored client-side in Alpine.js reactive data or `sessionStorage`) | [x] |

---

## 5 — Scientific Calculator Page

| # | Task | Status |
|---|------|--------|
| 5.1 | Create `Pages/Scientific.cshtml` + `Scientific.cshtml.cs` Razor Page | [x] |
| 5.2 | Extend standard layout with extra button rows: trig functions (`sin`, `cos`, `tan` and inverses), `log`, `ln`, `e^x`, `π`, `!` | [x] |
| 5.3 | Add angle-mode toggle (Degrees / Radians) managed with Alpine.js, sent as a parameter with each calculation request | [x] |
| 5.4 | Wire evaluate via HTMX same as standard calculator | [x] |
| 5.5 | Add parentheses and nested expression support in the UI | [x] |

---

## 6 — Graphing Calculator Page

| # | Task | Status |
|---|------|--------|
| 6.1 | Create `Pages/Graphing.cshtml` + `Graphing.cshtml.cs` Razor Page | [x] |
| 6.2 | Add a `<canvas>` element and initialise Chart.js (or D3.js) to render a 2-D coordinate plane with axes, grid, and labels | [x] |
| 6.3 | Create `Services/IGraphingService.cs` – accepts a function expression string, x-range, and step, returns `(x, y)[]` data points | [x] |
| 6.4 | Implement `Services/GraphingService.cs` using `ICalculatorEngine` for point evaluation | [x] |
| 6.5 | Build an equation input bar (Alpine.js) that lets users type/edit multiple equations, each with a colour picker | [x] |
| 6.6 | On submit, POST equations to a Razor handler via HTMX → return JSON data points → update the chart client-side | [x] |
| 6.7 | Add pan and zoom controls on the graph canvas | [x] |

### 6A — Inequalities

| # | Task | Status |
|---|------|--------|
| 6A.1 | Extend `IGraphingService` to detect inequality operators (`<`, `>`, `<=`, `>=`) in expressions | [x] |
| 6A.2 | Compute shaded region data for inequalities and return fill metadata | [x] |
| 6A.3 | Render shaded regions on the chart canvas with semi-transparent fills | [x] |

### 6B — Transforms

| # | Task | Status |
|---|------|--------|
| 6B.1 | Build a transforms panel where users can select a base function and choose transformation type (translate, reflect, stretch) | [x] |
| 6B.2 | Accept transformation parameters (e.g., horizontal shift `h`, vertical shift `k`, scale factor) | [x] |
| 6B.3 | Display the original and transformed functions on the same graph with distinct styles | [x] |

### 6C — Conic Graphing

| # | Task | Status |
|---|------|--------|
| 6C.1 | Add a conic-section input mode (circle, ellipse, parabola, hyperbola) with form fields for relevant parameters (centre, radii, etc.) | [x] |
| 6C.2 | Implement server-side point generation for each conic type | [x] |
| 6C.3 | Render conic sections on the chart, labelling key features (vertices, foci, directrix) | [x] |

---

## 7 — Geometry Calculator Page

| # | Task | Status |
|---|------|--------|
| 7.1 | Create `Pages/Geometry.cshtml` + `Geometry.cshtml.cs` Razor Page | [x] |
| 7.2 | Build a shape-selector panel (Circle, Triangle, Rectangle, Square, Trapezoid, Parallelogram, Ellipse, Sphere, Cylinder, Cone, Cube, Rectangular Prism) | [x] |
| 7.3 | For each shape, render an input form for its dimensions (e.g., radius, base, height) using Alpine.js to show/hide the correct form | [x] |
| 7.4 | Create `Services/IGeometryService.cs` with methods: `CalculateArea(...)`, `CalculatePerimeter(...)`, `CalculateVolume(...)`, `CalculateSurfaceArea(...)` | [x] |
| 7.5 | Implement `Services/GeometryService.cs` with all shape formulas | [x] |
| 7.6 | POST form via HTMX → return results partial → display computed values alongside a visual SVG/Canvas representation of the shape | [x] |
| 7.7 | Render an SVG diagram of each shape with labelled dimensions that updates reactively as inputs change | [x] |

---

## 7A — Statistics Calculator Page

> Provides comprehensive statistical analysis on user-supplied data sets.

| # | Task | Status |
|---|------|--------|
| 7A.1 | Create `Pages/Statistics.cshtml` + `Statistics.cshtml.cs` Razor Page | [x] |
| 7A.2 | Build a spreadsheet-style data-entry grid using Alpine.js — support add/remove rows & columns, column naming, inline editing, and clipboard paste | [x] |
| 7A.3 | Add CSV file upload via `<input type="file">` with HTMX `hx-post` to a Razor handler (`OnPostUploadCsv`); accept `.csv` files | [x] |
| 7A.4 | Implement CSV parsing in the handler using **CsvHelper** — detect headers, configurable delimiter, preview first N rows before full import | [x] |
| 7A.5 | Validate uploaded data: max row count limit (configurable in `appsettings.json`), numeric type checks, friendly error messages | [x] |
| 7A.6 | Create `Services/IStatisticsService.cs` with methods for descriptive stats: `Mean`, `Median`, `Mode`, `Range`, `Variance`, `StdDev`, `Min`, `Max`, `Sum`, `Count`, `Percentile`, `IQR`, `Skewness`, `Kurtosis` | [x] |
| 7A.7 | Implement `Services/StatisticsService.cs` with all descriptive statistics calculations | [x] |
| 7A.8 | Create `Services/IRegressionService.cs` — `LinearRegression`, `PolynomialRegression` returning equation, R² value, and fitted points | [x] |
| 7A.9 | Implement `Services/RegressionService.cs` | [x] |
| 7A.10 | Build a results panel that displays all computed statistics in a formatted table, updated via HTMX partial response | [x] |
| 7A.11 | Add data visualisation: histogram, box plot, scatter plot, line chart rendered with Chart.js / D3.js from the current data set | [x] |
| 7A.12 | Overlay regression trend lines and display equation + R² on scatter plots | [x] |
| 7A.13 | Add probability distribution calculators (Normal, Binomial, Poisson) — input parameters, compute probability, and render PDF/PMF chart | [x] |
| 7A.14 | Register `IStatisticsService` and `IRegressionService` in `Program.cs` | [x] |

---

## 7B — Data Management (Save / Load / Export)

> Authenticated users can persist data sets and graphing workspaces to the SQLite database.

| # | Task | Status |
|---|------|--------|
| 7B.1 | Create EF Core entity `Models/SavedDataSet.cs` — `Id`, `UserId`, `Name`, `Description`, `CreatedAt`, `UpdatedAt`, navigation to `DataRows` | [x] |
| 7B.2 | Create EF Core entity `Models/DataRow.cs` — `Id`, `DataSetId`, `RowIndex`, `ValuesJson` (JSON-serialised column values) | [x] |
| 7B.3 | Create EF Core entity `Models/SavedWorkspace.cs` — `Id`, `UserId`, `Name`, `WorkspaceType` (Graphing/Statistics), `ConfigurationJson`, `CreatedAt`, `UpdatedAt` | [x] |
| 7B.4 | Add `DbSet` properties to `ApplicationDbContext` and create an EF migration for the new tables | [x] |
| 7B.5 | Create `Services/IDataSetService.cs` — `SaveAsync`, `LoadAsync`, `ListByUserAsync`, `DeleteAsync`, `ExportCsvAsync` | [x] |
| 7B.6 | Implement `Services/DataSetService.cs` using `ApplicationDbContext` | [x] |
| 7B.7 | Add a **Save Data Set** button on the Statistics page (requires authentication; prompt login if anonymous) | [x] |
| 7B.8 | Create `Pages/Dashboard.cshtml` + `.cs` — list the current user's saved data sets and workspaces with load / rename / delete actions | [x] |
| 7B.9 | Add **Load Data Set** picker (HTMX modal or dropdown) on the Statistics page to load a saved set into the grid | [x] |
| 7B.10 | Add **Export CSV** button that downloads the current data set as a `.csv` file | [x] |
| 7B.11 | Add save/load support for Graphing calculator workspaces (equations + settings) using `SavedWorkspace` entity | [x] |
| 7B.12 | Protect all save/load/delete handlers with `[Authorize]` and verify resource ownership (`UserId` check) | [x] |

---

## 8 — Cross-Cutting Concerns

| # | Task | Status |
|---|------|--------|
| 8.1 | Add input sanitisation & anti-forgery token validation on all POST handlers | [—] |
| 8.2 | Add `[ValidateAntiForgeryToken]` or ensure Razor Pages default AF protection is active | [—] |
| 8.3 | Configure Content-Security-Policy headers to allow only required CDN sources and inline Alpine.js | [—] |
| 8.4 | Add structured logging via `ILogger<T>` in each service | [x] |
| 8.5 | Add error-handling middleware / custom error pages (400, 404, 500) | [x] |
| 8.6 | Add response caching for static computation results where appropriate | [—] |
| 8.7 | Set file-upload size limits and MIME-type validation for CSV uploads | [—] |
| 8.8 | Add account confirmation and password-reset email support (or defer with `RequireConfirmedAccount = false`) | [—] |
| 8.9 | Configure cookie authentication paths (LoginPath, AccessDeniedPath) | [x] |

---

## 9 — Testing

| # | Task | Status |
|---|------|--------|
| 9.1 | Create `TCalc.Tests` xUnit project, add project reference to `TCalc.Web` | [x] |
| 9.2 | Unit tests for `CalculatorEngine` (arithmetic, scientific functions, error handling) | [x] |
| 9.3 | Unit tests for `GraphingService` (data-point generation, inequality detection) | [x] |
| 9.4 | Unit tests for `GeometryService` (all shape calculations) | [x] |
| 9.5 | Unit tests for `StatisticsService` (descriptive stats, edge cases: empty set, single value, all identical) | [x] |
| 9.6 | Unit tests for `RegressionService` (linear fit, polynomial fit, R² accuracy) | [x] |
| 9.7 | Unit tests for `DataSetService` (save, load, delete, export CSV) using in-memory SQLite provider | [ ] |
| 9.8 | Integration tests for CSV upload handler (valid file, invalid file, oversized file) | [ ] |
| 9.9 | Integration tests for Identity flows (register, login, access protected page) | [ ] |
| 9.10 | Integration tests for Razor Page handlers using `WebApplicationFactory<Program>` | [ ] |
| 9.11 | ~~Add a CI build script or GitHub Actions workflow that restores, builds, and runs tests~~ | [—] |

---

## 10 — Polish & Stretch Goals

| # | Task | Status |
|---|------|--------|
| 10.1 | Animate calculator button presses with CSS transitions | [x] |
| 10.2 | Add haptic feedback hint (`navigator.vibrate`) on mobile button taps | [x] |
| 10.3 | Add a "Copy result" button that writes the answer to the clipboard | [x] |
| 10.4 | Add keyboard shortcut help modal (accessible from `?` key) | [x] |
| 10.5 | Lighthouse audit – target ≥ 90 on Performance, Accessibility, Best Practices, SEO | [—] |
| 10.6 | Publish-ready `Dockerfile` and/or Azure App Service deployment guide | [—] |

---

## Recommended Implementation Order

```
Phase 1  – Foundations       → §0 (0.1–0.6), §1, §2       (layout, theming, client libraries)
Phase 2  – Database & Auth   → §0 (0.7–0.13)              (SQLite, EF Core, Identity)
Phase 3  – Core Engine       → §3                          (back-end maths engine + tests)
Phase 4  – Standard Calc     → §4                          (first visible feature)
Phase 5  – Scientific        → §5                          (builds on standard)
Phase 6  – Graphing          → §6, §6A, §6B, §6C          (most complex feature)
Phase 7  – Geometry          → §7                          (independent of graphing)
Phase 8  – Statistics        → §7A                         (statistics engine, data entry, CSV upload)
Phase 9  – Data Management   → §7B                         (save/load/export, dashboard)
Phase 10 – Hardening         → §8, §9                      (security, testing, CI)
Phase 11 – Polish            → §10                         (UX refinements)
```

---

## File / Folder Structure (Target)

```
TCalc.Web/
├── Data/
│   └── ApplicationDbContext.cs         ← EF Core context (IdentityDbContext)
├── Models/
│   ├── SavedDataSet.cs                 ← user-owned data set entity
│   ├── DataRow.cs                      ← data set row entity
│   └── SavedWorkspace.cs               ← saved graphing/stats workspace entity
├── Migrations/                         ← EF Core auto-generated migrations
├── Pages/
│   ├── Account/                        ← Identity pages (Register, Login, Logout, etc.)
│   ├── Shared/
│   │   ├── _Layout.cshtml              ← updated layout with auth links
│   │   ├── _Layout.cshtml.css          ← scoped styles
│   │   ├── _LoginPartial.cshtml        ← login/register/logout navbar partial
│   │   ├── _CalculatorButtons.cshtml   ← shared button partial
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Index.cshtml / .cs              ← landing/home page
│   ├── Standard.cshtml / .cs           ← standard calculator
│   ├── Scientific.cshtml / .cs         ← scientific calculator
│   ├── Graphing.cshtml / .cs           ← graphing calculator
│   ├── Geometry.cshtml / .cs           ← geometry calculator
│   ├── Statistics.cshtml / .cs         ← statistics calculator (data entry, CSV upload, analysis)
│   ├── Dashboard.cshtml / .cs          ← user's saved data sets & workspaces
│   ├── Privacy.cshtml / .cs
│   └── Error.cshtml / .cs
├── Services/
│   ├── ICalculatorEngine.cs
│   ├── CalculatorEngine.cs
│   ├── IGraphingService.cs
│   ├── GraphingService.cs
│   ├── IGeometryService.cs
│   ├── GeometryService.cs
│   ├── IStatisticsService.cs           ← descriptive stats
│   ├── StatisticsService.cs
│   ├── IProbabilityService.cs          ← Normal, Binomial, Poisson distributions
│   ├── ProbabilityService.cs
│   ├── IRegressionService.cs           ← linear & polynomial regression
│   ├── RegressionService.cs
│   ├── IDataSetService.cs              ← CRUD + CSV export for saved data sets
│   └── DataSetService.cs
├── wwwroot/
│   ├── css/
│   │   └── site.css                    ← theme variables, dark mode
│   ├── js/
│   │   └── site.js                     ← shared JS helpers
│   └── lib/
│       ├── alpine.js/
│       ├── htmx/
│       ├── chart.js/ (or d3/)
│       └── bootstrap/
├── Specifications/
│   ├── Overview.md
│   └── ImplementationGuide.md          ← this file
├── Program.cs
├── TCalc.Web.csproj
├── libman.json
├── tcalc.db                            ← SQLite database file (gitignored)
├── appsettings.json
└── appsettings.Development.json

TCalc.Tests/
├── CalculatorEngineTests.cs
├── GraphingServiceTests.cs
├── GeometryServiceTests.cs
├── StatisticsServiceTests.cs
├── RegressionServiceTests.cs
├── ProbabilityServiceTests.cs
├── DataSetServiceTests.cs
├── CsvUploadTests.cs
├── IdentityIntegrationTests.cs
├── PageIntegrationTests.cs
└── TCalc.Tests.csproj
```

---

*Last updated: 2025-07-12*
