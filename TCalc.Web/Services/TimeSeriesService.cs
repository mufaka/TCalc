namespace TCalc.Web.Services;

/// <summary>
/// Implements time series decomposition, autocorrelation, simple forecasting, and 2-D PCA.
/// </summary>
public sealed class TimeSeriesService : ITimeSeriesService
{
    private readonly ILogger<TimeSeriesService> _logger;

    public TimeSeriesService(ILogger<TimeSeriesService> logger) => _logger = logger;

    // ── Decomposition ───────────────────────────────────────────────────

    public DecompositionResult Decompose(double[] values, int seasonalPeriod)
    {
        _logger.LogDebug("Decomposing series of length {N} with period {P}", values?.Length ?? 0, seasonalPeriod);

        if (values is null || values.Length < 4)
            return DecompositionResult.Fail("At least 4 data points are required.");
        if (seasonalPeriod < 2)
            return DecompositionResult.Fail("Seasonal period must be at least 2.");
        if (values.Length < seasonalPeriod * 2)
            return DecompositionResult.Fail($"Need at least {seasonalPeriod * 2} data points for period {seasonalPeriod}.");

        int n = values.Length;
        int halfWindow = seasonalPeriod / 2;

        // Step 1: Centred moving average for trend
        var trend = new double?[n];
        for (int i = halfWindow; i < n - halfWindow; i++)
        {
            double sum = 0;
            int count = 0;
            if (seasonalPeriod % 2 == 0)
            {
                // Even period: average of two offset windows
                for (int j = i - halfWindow; j < i + halfWindow; j++)
                {
                    sum += values[j];
                    count++;
                }
                sum += values[i + halfWindow];
                count++;
                // Use 2×m centred average
                sum = 0; count = 0;
                for (int j = i - halfWindow; j <= i + halfWindow; j++)
                {
                    double weight = (j == i - halfWindow || j == i + halfWindow) ? 0.5 : 1.0;
                    sum += values[j] * weight;
                    count++;
                }
                trend[i] = sum / seasonalPeriod;
            }
            else
            {
                for (int j = i - halfWindow; j <= i + halfWindow; j++)
                {
                    sum += values[j];
                    count++;
                }
                trend[i] = sum / count;
            }
        }

        // Step 2: Detrended = Original − Trend
        var detrended = new double?[n];
        for (int i = 0; i < n; i++)
        {
            detrended[i] = trend[i].HasValue ? values[i] - trend[i].Value : null;
        }

        // Step 3: Average seasonal component per position within the period
        var seasonalAvg = new double[seasonalPeriod];
        var seasonalCount = new int[seasonalPeriod];
        for (int i = 0; i < n; i++)
        {
            if (detrended[i].HasValue)
            {
                seasonalAvg[i % seasonalPeriod] += detrended[i]!.Value;
                seasonalCount[i % seasonalPeriod]++;
            }
        }
        for (int j = 0; j < seasonalPeriod; j++)
        {
            seasonalAvg[j] = seasonalCount[j] > 0 ? seasonalAvg[j] / seasonalCount[j] : 0;
        }

        // Centre the seasonal component (sum to 0)
        double seasonalMean = seasonalAvg.Average();
        for (int j = 0; j < seasonalPeriod; j++)
            seasonalAvg[j] -= seasonalMean;

        // Step 4: Seasonal and Residual
        var seasonal = new double?[n];
        var residual = new double?[n];
        for (int i = 0; i < n; i++)
        {
            seasonal[i] = seasonalAvg[i % seasonalPeriod];
            residual[i] = trend[i].HasValue ? values[i] - trend[i].Value - seasonal[i]!.Value : null;
        }

        return new DecompositionResult
        {
            Success = true,
            Original = values,
            Trend = trend,
            Seasonal = seasonal,
            Residual = residual,
            SeasonalPeriod = seasonalPeriod,
        };
    }

    // ── Autocorrelation ─────────────────────────────────────────────────

    public AutocorrelationResult Autocorrelation(double[] values, int maxLag)
    {
        _logger.LogDebug("Computing ACF for series of length {N}, maxLag={Lag}", values?.Length ?? 0, maxLag);

        if (values is null || values.Length < 3)
            return AutocorrelationResult.Fail("At least 3 data points are required.");
        if (maxLag < 1)
            return AutocorrelationResult.Fail("Max lag must be at least 1.");

        int n = values.Length;
        maxLag = Math.Min(maxLag, n - 1);

        double mean = values.Average();
        double variance = 0;
        for (int i = 0; i < n; i++)
            variance += (values[i] - mean) * (values[i] - mean);

        if (variance < 1e-15)
            return AutocorrelationResult.Fail("Series has zero variance (constant values).");

        var lags = new int[maxLag + 1];
        var acfValues = new double[maxLag + 1];

        for (int lag = 0; lag <= maxLag; lag++)
        {
            lags[lag] = lag;
            double cov = 0;
            for (int i = 0; i < n - lag; i++)
                cov += (values[i] - mean) * (values[i + lag] - mean);
            acfValues[lag] = cov / variance;
        }

        return new AutocorrelationResult
        {
            Success = true,
            Lags = lags,
            Values = acfValues,
            SignificanceBand = 1.96 / Math.Sqrt(n),
        };
    }

    // ── Forecast (Simple Exponential Smoothing) ─────────────────────────

    public ForecastResult ForecastSimple(double[] values, int horizon, double alpha = 0.3)
    {
        _logger.LogDebug("Forecasting {H} periods with alpha={A} for series of length {N}",
            horizon, alpha, values?.Length ?? 0);

        if (values is null || values.Length < 2)
            return ForecastResult.Fail("At least 2 data points are required.");
        if (horizon < 1 || horizon > 100)
            return ForecastResult.Fail("Forecast horizon must be between 1 and 100.");
        if (alpha <= 0 || alpha >= 1)
            return ForecastResult.Fail("Smoothing parameter α must be between 0 and 1 (exclusive).");

        int n = values.Length;

        // Simple exponential smoothing
        double[] smoothed = new double[n];
        smoothed[0] = values[0];
        for (int i = 1; i < n; i++)
            smoothed[i] = alpha * values[i] + (1 - alpha) * smoothed[i - 1];

        // Forecast: flat line at last smoothed value
        double lastSmoothed = smoothed[n - 1];

        // Compute residuals for prediction interval
        double sse = 0;
        for (int i = 1; i < n; i++)
        {
            double err = values[i] - smoothed[i];
            sse += err * err;
        }
        double rmse = Math.Sqrt(sse / (n - 1));

        var forecast = new double[horizon];
        var upper = new double[horizon];
        var lower = new double[horizon];

        for (int h = 0; h < horizon; h++)
        {
            forecast[h] = lastSmoothed;
            // Prediction interval widens with horizon
            double width = 1.96 * rmse * Math.Sqrt(1 + h * alpha * alpha);
            upper[h] = lastSmoothed + width;
            lower[h] = lastSmoothed - width;
        }

        return new ForecastResult
        {
            Success = true,
            Historical = values,
            Forecast = forecast,
            UpperBound = upper,
            LowerBound = lower,
            Alpha = alpha,
        };
    }

    // ── PCA 2-D ─────────────────────────────────────────────────────────

    public PcaResult Pca2D(double[] x, double[] y)
    {
        _logger.LogDebug("Computing 2-D PCA on {N} points", x?.Length ?? 0);

        if (x is null || y is null || x.Length != y.Length)
            return PcaResult.Fail("X and Y arrays must be non-null and equal length.");
        if (x.Length < 3)
            return PcaResult.Fail("At least 3 data points are required for PCA.");

        int n = x.Length;

        // Mean-centre
        double meanX = x.Average();
        double meanY = y.Average();
        var cx = new double[n];
        var cy = new double[n];
        for (int i = 0; i < n; i++)
        {
            cx[i] = x[i] - meanX;
            cy[i] = y[i] - meanY;
        }

        // 2×2 covariance matrix
        double cxx = 0, cxy = 0, cyy = 0;
        for (int i = 0; i < n; i++)
        {
            cxx += cx[i] * cx[i];
            cxy += cx[i] * cy[i];
            cyy += cy[i] * cy[i];
        }
        cxx /= (n - 1);
        cxy /= (n - 1);
        cyy /= (n - 1);

        // Eigenvalues of 2×2 matrix [[cxx, cxy], [cxy, cyy]]
        // λ = ((cxx+cyy) ± sqrt((cxx-cyy)² + 4·cxy²)) / 2
        double trace = cxx + cyy;
        double det = cxx * cyy - cxy * cxy;
        double discriminant = trace * trace - 4 * det;
        if (discriminant < 0) discriminant = 0; // numerical safety

        double sqrtDisc = Math.Sqrt(discriminant);
        double eigenvalue1 = (trace + sqrtDisc) / 2; // larger
        double eigenvalue2 = (trace - sqrtDisc) / 2; // smaller
        if (eigenvalue2 < 0) eigenvalue2 = 0;

        double totalVariance = eigenvalue1 + eigenvalue2;
        double explainedVar1 = totalVariance > 0 ? eigenvalue1 / totalVariance : 1;
        double explainedVar2 = totalVariance > 0 ? eigenvalue2 / totalVariance : 0;

        // Eigenvectors
        double pc1x, pc1y, pc2x, pc2y;
        if (Math.Abs(cxy) > 1e-15)
        {
            pc1x = eigenvalue1 - cyy;
            pc1y = cxy;
            double norm1 = Math.Sqrt(pc1x * pc1x + pc1y * pc1y);
            pc1x /= norm1;
            pc1y /= norm1;

            pc2x = eigenvalue2 - cyy;
            pc2y = cxy;
            double norm2 = Math.Sqrt(pc2x * pc2x + pc2y * pc2y);
            pc2x /= norm2;
            pc2y /= norm2;
        }
        else
        {
            // Already aligned with axes
            if (cxx >= cyy)
            {
                pc1x = 1; pc1y = 0;
                pc2x = 0; pc2y = 1;
            }
            else
            {
                pc1x = 0; pc1y = 1;
                pc2x = 1; pc2y = 0;
            }
        }

        // Project onto PC1
        var projX = new double[n];
        var projY = new double[n];
        for (int i = 0; i < n; i++)
        {
            double dot = cx[i] * pc1x + cy[i] * pc1y;
            projX[i] = meanX + dot * pc1x;
            projY[i] = meanY + dot * pc1y;
        }

        return new PcaResult
        {
            Success = true,
            CentredX = cx,
            CentredY = cy,
            MeanX = meanX,
            MeanY = meanY,
            Pc1X = pc1x,
            Pc1Y = pc1y,
            Pc2X = pc2x,
            Pc2Y = pc2y,
            Eigenvalue1 = eigenvalue1,
            Eigenvalue2 = eigenvalue2,
            ExplainedVariance1 = explainedVar1,
            ExplainedVariance2 = explainedVar2,
            ProjectedX = projX,
            ProjectedY = projY,
        };
    }
}
