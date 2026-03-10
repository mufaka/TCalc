using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class ScientificModel : PageModel
{
    private readonly ICalculatorEngine _engine;

    public ScientificModel(ICalculatorEngine engine)
    {
        _engine = engine;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPostCalculate([FromForm] string expression, [FromForm] string angleMode)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return Partial("_CalculatorResult", CalculationResult.Fail("Empty expression.", ""));

        var mode = string.Equals(angleMode, "deg", StringComparison.OrdinalIgnoreCase)
            ? AngleMode.Degrees
            : AngleMode.Radians;

        var result = _engine.Evaluate(expression, mode);
        return Partial("_CalculatorResult", result);
    }
}
