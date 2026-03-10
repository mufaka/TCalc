using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCalc.Web.Models;
using TCalc.Web.Services;

namespace TCalc.Web.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IDataSetService _dataSetService;

    public DashboardModel(IDataSetService dataSetService)
    {
        _dataSetService = dataSetService;
    }

    public List<SavedDataSet> DataSets { get; set; } = [];
    public List<SavedWorkspace> Workspaces { get; set; } = [];

    public async Task OnGetAsync()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        DataSets = await _dataSetService.ListByUserAsync(userId);
        Workspaces = await _dataSetService.ListWorkspacesByUserAsync(userId);
    }

    public async Task<IActionResult> OnPostDeleteDataSetAsync([FromForm] int id)
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _dataSetService.DeleteAsync(id, userId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteWorkspaceAsync([FromForm] int id)
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _dataSetService.DeleteWorkspaceAsync(id, userId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetExportCsvAsync(int id)
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        string csv = await _dataSetService.ExportCsvAsync(id, userId);
        if (string.IsNullOrEmpty(csv))
            return NotFound();

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"dataset-{id}.csv");
    }
}
