using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class StatisticsModel : PageModel
{
    private readonly IStatisticsService _stats;
    private readonly IRegressionService _regression;
    private readonly IProbabilityService _probability;
    private readonly IDataSetService _dataSetService;
    private readonly ITimeSeriesService _timeSeries;
    private readonly IConfiguration _config;

    public StatisticsModel(
        IStatisticsService stats,
        IRegressionService regression,
        IProbabilityService probability,
        IDataSetService dataSetService,
        ITimeSeriesService timeSeries,
        IConfiguration config)
    {
        _stats = stats;
        _regression = regression;
        _probability = probability;
        _dataSetService = dataSetService;
        _timeSeries = timeSeries;
        _config = config;
    }

    /// <summary>
    /// JSON payload to pre-populate Alpine state when loading a saved data set.
    /// </summary>
    public string? PreloadJson { get; set; }

    public async Task OnGetAsync(int? loadId)
    {
        if (loadId.HasValue && User.Identity?.IsAuthenticated == true)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var dataSet = await _dataSetService.LoadAsync(loadId.Value, userId);
            if (dataSet is not null)
            {
                string[]? headers = dataSet.HeadersJson is not null
                    ? JsonSerializer.Deserialize<string[]>(dataSet.HeadersJson)
                    : null;

                var columns = new Dictionary<string, List<string>>();
                if (headers is not null)
                {
                    foreach (string h in headers)
                        columns[h] = [];

                    foreach (var row in dataSet.Rows)
                    {
                        string[]? values = JsonSerializer.Deserialize<string[]>(row.ValuesJson);
                        if (values is not null)
                        {
                            for (int i = 0; i < headers.Length && i < values.Length; i++)
                                columns[headers[i]].Add(values[i]);
                        }
                    }
                }

                PreloadJson = JsonSerializer.Serialize(new
                {
                    headers,
                    columns,
                    rowCount = dataSet.Rows.Count,
                    name = dataSet.Name,
                });
            }
        }
    }

    /// <summary>
    /// Computes descriptive statistics for the supplied data column.
    /// </summary>
    public IActionResult OnPostCompute([FromForm] string data)
    {
        double[]? values = ParseData(data);
        if (values is null || values.Length == 0)
            return Partial("_StatisticsResult", StatisticsResult.Fail("Enter at least one numeric value."));

        var result = _stats.Compute(values);
        return Partial("_StatisticsResult", result);
    }

    /// <summary>
    /// Returns JSON for Chart.js — histogram, box plot data, and the raw values.
    /// </summary>
    public IActionResult OnPostVisualize([FromForm] string data)
    {
        double[]? values = ParseData(data);
        if (values is null || values.Length == 0)
            return new JsonResult(new { error = "No valid data." });

        var stats = _stats.Compute(values);
        if (!stats.Success)
            return new JsonResult(new { error = stats.Error });

        // Histogram bins (Sturges' rule)
        int binCount = Math.Max(1, (int)Math.Ceiling(Math.Log2(values.Length) + 1));
        double binWidth = stats.Range / binCount;
        if (binWidth <= 0) { binCount = 1; binWidth = 1; }

        var bins = new List<object>();
        for (int i = 0; i < binCount; i++)
        {
            double lo = stats.Min + i * binWidth;
            double hi = lo + binWidth;
            int count = values.Count(v => v >= lo && (i == binCount - 1 ? v <= hi : v < hi));
            bins.Add(new { label = $"{lo:G4}–{hi:G4}", count });
        }

        return new JsonResult(new
        {
            values = values.OrderBy(x => x).ToArray(),
            stats = new
            {
                stats.Min,
                stats.Max,
                stats.Q1,
                stats.Median,
                stats.Q3,
                stats.Mean,
                stats.StdDev
            },
            histogram = bins,
        });
    }

    /// <summary>
    /// Computes regression for two-column data and returns JSON for Chart.js.
    /// </summary>
    public IActionResult OnPostRegression([FromForm] string dataX, [FromForm] string dataY, [FromForm] int degree = 1)
    {
        double[]? xVals = ParseData(dataX);
        double[]? yVals = ParseData(dataY);

        if (xVals is null || yVals is null || xVals.Length == 0 || yVals.Length == 0)
            return new JsonResult(new { error = "Provide both X and Y data columns." });

        if (xVals.Length != yVals.Length)
            return new JsonResult(new { error = $"X has {xVals.Length} values but Y has {yVals.Length}. They must match." });

        var result = degree <= 1
            ? _regression.LinearRegression(xVals, yVals)
            : _regression.PolynomialRegression(xVals, yVals, degree);

        if (!result.Success)
            return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            equation = result.Equation,
            rSquared = result.RSquared,
            scatter = xVals.Zip(yVals, (x, y) => new { x, y }).ToArray(),
            trendLine = result.FittedPoints.Select(p => new { x = p.X, y = p.Y }).ToArray(),
        });
    }

    /// <summary>
    /// Parses a CSV file upload and returns the data as JSON columns.
    /// </summary>
    public async Task<IActionResult> OnPostUploadCsv(IFormFile? csvFile)
    {
        if (csvFile is null || csvFile.Length == 0)
            return new JsonResult(new { error = "No file selected." });

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            && !csvFile.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return new JsonResult(new { error = "Only .csv and .txt files are supported." });

        int maxRows = _config.GetValue("CsvUpload:MaxRowCount", 10_000);

        try
        {
            using var reader = new StreamReader(csvFile.OpenReadStream());
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
            };
            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();
            string[]? headers = csv.HeaderRecord;
            if (headers is null || headers.Length == 0)
                return new JsonResult(new { error = "CSV file has no headers." });

            var columns = new Dictionary<string, List<string>>();
            foreach (string h in headers)
                columns[h] = [];

            int rowCount = 0;
            while (await csv.ReadAsync())
            {
                if (++rowCount > maxRows)
                    return new JsonResult(new { error = $"File exceeds the maximum of {maxRows:N0} rows." });

                for (int i = 0; i < headers.Length; i++)
                    columns[headers[i]].Add(csv.GetField(i) ?? "");
            }

            return new JsonResult(new { headers, columns, rowCount });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = $"Failed to parse CSV: {ex.Message}" });
        }
    }

    /// <summary>
    /// Saves the current data to the database. Requires authentication.
    /// </summary>
    public async Task<IActionResult> OnPostSaveDataSet(
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] string headersJson,
        [FromForm] string columnsJson)
    {
        if (User.Identity?.IsAuthenticated != true)
            return new JsonResult(new { error = "You must be logged in to save data sets." }) { StatusCode = 401 };

        if (string.IsNullOrWhiteSpace(name))
            return new JsonResult(new { error = "Name is required." });

        try
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            string[]? headers = JsonSerializer.Deserialize<string[]>(headersJson);
            var columns = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(columnsJson);

            if (headers is null || columns is null || headers.Length == 0)
                return new JsonResult(new { error = "No data to save." });

            int rowCount = columns.Values.FirstOrDefault()?.Count ?? 0;
            var rows = new List<string[]>();
            for (int r = 0; r < rowCount; r++)
            {
                var row = new string[headers.Length];
                for (int c = 0; c < headers.Length; c++)
                    row[c] = columns.TryGetValue(headers[c], out var col) && r < col.Count ? col[r] : "";
                rows.Add(row);
            }

            var dataSet = await _dataSetService.SaveAsync(userId, name, description, headers, rows);
            return new JsonResult(new { success = true, id = dataSet.Id });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = $"Save failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Exports a saved data set as a CSV file download.
    /// </summary>
    public async Task<IActionResult> OnGetExportCsv(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        string csv = await _dataSetService.ExportCsvAsync(id, userId);
        if (string.IsNullOrEmpty(csv))
            return NotFound();

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"dataset-{id}.csv");
    }

    /// <summary>
    /// Lists saved data sets for the current user (for the load picker).
    /// </summary>
    public async Task<IActionResult> OnGetListDataSets()
    {
        if (User.Identity?.IsAuthenticated != true)
            return new JsonResult(new { error = "Not authenticated." }) { StatusCode = 401 };

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var sets = await _dataSetService.ListByUserAsync(userId);
        return new JsonResult(sets.Select(s => new
        {
            s.Id,
            s.Name,
            s.Description,
            updatedAt = s.UpdatedAt.ToString("yyyy-MM-dd HH:mm"),
        }));
    }

    // ─── helpers ────────────────────────────────────────────────

    /// <summary>
    /// Computes probability distribution results.
    /// </summary>
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

    // ── Time Series ─────────────────────────────────────────────────

    public IActionResult OnPostDecompose(
        [FromForm] string data,
        [FromForm] int seasonalPeriod)
    {
        double[]? values = ParseData(data);
        if (values is null || values.Length == 0)
            return new JsonResult(new { error = "Enter time series data." });

        var result = _timeSeries.Decompose(values, seasonalPeriod);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            original = result.Original,
            trend = result.Trend,
            seasonal = result.Seasonal,
            residual = result.Residual,
            seasonalPeriod = result.SeasonalPeriod,
        });
    }

    public IActionResult OnPostAutocorrelation(
        [FromForm] string data,
        [FromForm] int maxLag)
    {
        double[]? values = ParseData(data);
        if (values is null || values.Length == 0)
            return new JsonResult(new { error = "Enter time series data." });

        var result = _timeSeries.Autocorrelation(values, maxLag);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            lags = result.Lags,
            values = result.Values,
            significanceBand = result.SignificanceBand,
        });
    }

    public IActionResult OnPostForecast(
        [FromForm] string data,
        [FromForm] int horizon,
        [FromForm] double alpha)
    {
        double[]? values = ParseData(data);
        if (values is null || values.Length == 0)
            return new JsonResult(new { error = "Enter time series data." });

        var result = _timeSeries.ForecastSimple(values, horizon, alpha);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            historical = result.Historical,
            forecast = result.Forecast,
            upperBound = result.UpperBound,
            lowerBound = result.LowerBound,
            result.Alpha,
        });
    }

    // ── PCA ─────────────────────────────────────────────────────────

    public IActionResult OnPostPca(
        [FromForm] string dataX,
        [FromForm] string dataY)
    {
        double[]? xVals = ParseData(dataX);
        double[]? yVals = ParseData(dataY);

        if (xVals is null || yVals is null || xVals.Length == 0 || yVals.Length == 0)
            return new JsonResult(new { error = "Provide both X and Y data." });

        if (xVals.Length != yVals.Length)
            return new JsonResult(new { error = $"X has {xVals.Length} values but Y has {yVals.Length}." });

        var result = _timeSeries.Pca2D(xVals, yVals);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.MeanX,
            result.MeanY,
            pc1X = result.Pc1X,
            pc1Y = result.Pc1Y,
            pc2X = result.Pc2X,
            pc2Y = result.Pc2Y,
            result.Eigenvalue1,
            result.Eigenvalue2,
            result.ExplainedVariance1,
            result.ExplainedVariance2,
            projectedX = result.ProjectedX,
            projectedY = result.ProjectedY,
            originalX = xVals,
            originalY = yVals,
        });
    }

    // ── Multiple Linear Regression ──────────────────────────────────

    public IActionResult OnPostMultipleRegression(
        [FromForm] string columnsJson,
        [FromForm] string dataY)
    {
        double[]? yVals = ParseData(dataY);
        if (yVals is null || yVals.Length == 0)
            return new JsonResult(new { error = "Y data is required." });

        double[][]? xMatrix;
        try
        {
            xMatrix = JsonSerializer.Deserialize<double[][]>(columnsJson);
        }
        catch
        {
            return new JsonResult(new { error = "Invalid predictor data format." });
        }

        if (xMatrix is null || xMatrix.Length == 0)
            return new JsonResult(new { error = "Predictor data is required." });

        // xMatrix comes as columns — transpose to rows
        int numPredictors = xMatrix.Length;
        int numObs = xMatrix[0].Length;
        if (numObs != yVals.Length)
            return new JsonResult(new { error = $"Predictor columns have {numObs} values but Y has {yVals.Length}." });

        var rows = new double[numObs][];
        for (int i = 0; i < numObs; i++)
        {
            rows[i] = new double[numPredictors];
            for (int j = 0; j < numPredictors; j++)
                rows[i][j] = xMatrix[j][i];
        }

        var result = _regression.MultipleLinearRegression(rows, yVals);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.Equation,
            result.RSquared,
            result.AdjustedRSquared,
            result.Coefficients,
            result.StandardErrors,
            result.TStatistics,
            result.FittedValues,
            result.Residuals,
        });
    }

    // ── Logistic Regression ─────────────────────────────────────────

    public IActionResult OnPostLogisticRegression(
        [FromForm] string dataX,
        [FromForm] string dataY)
    {
        double[]? xVals = ParseData(dataX);
        if (xVals is null || xVals.Length == 0)
            return new JsonResult(new { error = "X data is required." });

        bool[]? yVals;
        try
        {
            var rawY = ParseData(dataY);
            if (rawY is null || rawY.Length == 0)
                return new JsonResult(new { error = "Y data is required." });
            yVals = rawY.Select(v => v >= 0.5).ToArray();
        }
        catch
        {
            return new JsonResult(new { error = "Invalid Y data." });
        }

        if (xVals.Length != yVals.Length)
            return new JsonResult(new { error = $"X has {xVals.Length} values but Y has {yVals.Length}." });

        var result = _regression.LogisticRegression(xVals, yVals);
        if (!result.Success) return new JsonResult(new { error = result.Error });

        return new JsonResult(new
        {
            result.Beta0,
            result.Beta1,
            result.Accuracy,
            result.PredictedProbabilities,
            sigmoidCurve = result.SigmoidCurve.Select(p => new { p.X, p.Y }),
            scatterX = xVals,
            scatterY = yVals.Select(b => b ? 1.0 : 0.0),
        });
    }
}
