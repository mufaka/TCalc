using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

public class StandardModel : PageModel
{
    private readonly ICalculatorEngine _engine;

    public StandardModel(ICalculatorEngine engine)
    {
        _engine = engine;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPostCalculate([FromForm] string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return Partial("_CalculatorResult", CalculationResult.Fail("Empty expression.", ""));

        var result = _engine.Evaluate(expression);
        return Partial("_CalculatorResult", result);
    }
}
