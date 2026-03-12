using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TCalc.Web.Data;

namespace TCalc.Tests;

public class PageIntegrationTests : IClassFixture<PageIntegrationTests.TCalcWebFactory>
{
    private readonly HttpClient _client;

    public PageIntegrationTests(TCalcWebFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  Page GET requests return 200 OK
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("/")]
    [InlineData("/Distributions")]
    [InlineData("/Simulation")]
    [InlineData("/Inference")]
    [InlineData("/Probability")]
    [InlineData("/Statistics")]
    [InlineData("/Standard")]
    [InlineData("/Scientific")]
    [InlineData("/Graphing")]
    [InlineData("/Geometry")]
    public async Task Page_Get_ReturnsSuccess(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Distributions")]
    [InlineData("/Simulation")]
    [InlineData("/Inference")]
    public async Task Page_Get_ReturnsHtml(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  Distributions page handlers
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Distributions_PostProbability_Normal_ReturnsJson()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["distribution"] = "normal",
            ["mean"] = "0",
            ["stdDev"] = "1",
            ["x"] = "0",
        });

        var response = await PostWithAntiForgery("/Distributions", "Probability", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("normal", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Distributions_PostProbability_Binomial_ReturnsJson()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["distribution"] = "binomial",
            ["n"] = "10",
            ["p"] = "0.5",
            ["k"] = "5",
        });

        var response = await PostWithAntiForgery("/Distributions", "Probability", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("binomial", json, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  Simulation page handlers
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Simulation_PostEstimatePi_ReturnsJson()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["sampleCount"] = "1000",
        });

        var response = await PostWithAntiForgery("/Simulation", "EstimatePi", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("piEstimate", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Simulation_PostIntegrate_ReturnsJson()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["functionName"] = "x^2",
            ["a"] = "0",
            ["b"] = "1",
            ["sampleCount"] = "1000",
        });

        var response = await PostWithAntiForgery("/Simulation", "Integrate", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("estimate", json, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  Inference page handlers
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Inference_PostBayesianUpdate_ReturnsJson()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["priorAlpha"] = "1",
            ["priorBeta"] = "1",
            ["successes"] = "7",
            ["trials"] = "10",
        });

        var response = await PostWithAntiForgery("/Inference", "BayesianUpdate", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("posteriorAlpha", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inference_PostZTest_ReturnsJson()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["sampleMean"] = "105",
            ["populationMean"] = "100",
            ["populationStdDev"] = "15",
            ["sampleSize"] = "36",
            ["alpha"] = "0.05",
            ["alternative"] = "two-tailed",
        });

        var response = await PostWithAntiForgery("/Inference", "ZTest", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("testStatistic", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inference_PostConfidenceInterval_ReturnsJson()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["sampleMean"] = "100",
            ["stdDev"] = "15",
            ["sampleSize"] = "36",
            ["confidenceLevel"] = "0.95",
            ["isPopulationStdDev"] = "true",
        });

        var response = await PostWithAntiForgery("/Inference", "ConfidenceInterval", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("lowerBound", json, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helper: POST with anti-forgery token
    // ════════════════════════════════════════════════════════════════

    private async Task<HttpResponseMessage> PostWithAntiForgery(
        string pageUrl, string handler, FormUrlEncodedContent formData)
    {
        // GET the page to extract the anti-forgery token
        var getResponse = await _client.GetAsync(pageUrl);
        getResponse.EnsureSuccessStatusCode();

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        // Rebuild form data with the token
        var fields = await formData.ReadAsStringAsync();
        var allFields = string.IsNullOrEmpty(token)
            ? fields
            : $"__RequestVerificationToken={Uri.EscapeDataString(token)}&{fields}";

        var content = new StringContent(allFields, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        // Set cookies from the GET response
        var postUrl = $"{pageUrl}?handler={handler}";
        return await _client.PostAsync(postUrl, content);
    }

    private static string? ExtractAntiForgeryToken(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
        {
            // Try alternate attribute order
            const string alt = "__RequestVerificationToken\" value=\"";
            idx = html.IndexOf(alt, StringComparison.Ordinal);
            if (idx < 0) return null;
            idx += alt.Length;
        }
        else
        {
            idx += marker.Length;
        }

        var end = html.IndexOf('"', idx);
        return end > idx ? html[idx..end] : null;
    }

    // ════════════════════════════════════════════════════════════════
    //  Test Web Application Factory
    // ════════════════════════════════════════════════════════════════

    public class TCalcWebFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the production DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TCalcTestDb"));
            });

            builder.UseEnvironment("Development");
        }
    }
}
