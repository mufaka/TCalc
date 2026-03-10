using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class GeometryModel : PageModel
{
    private readonly IGeometryService _geometry;

    public GeometryModel(IGeometryService geometry)
    {
        _geometry = geometry;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPostCalculate([FromForm] string shape, [FromForm] Dictionary<string, double> dims)
    {
        if (string.IsNullOrWhiteSpace(shape))
            return Partial("_GeometryResult", new GeometryResult { Success = false, Error = "Select a shape." });

        var result = _geometry.Calculate(shape, dims ?? new Dictionary<string, double>());
        return Partial("_GeometryResult", result);
    }
}
