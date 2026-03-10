using TCalc.Web.Models;

namespace TCalc.Web.Services;

/// <summary>
/// CRUD and CSV-export operations for user-owned data sets and workspaces.
/// </summary>
public interface IDataSetService
{
    Task<SavedDataSet> SaveAsync(string userId, string name, string? description, string[] headers, List<string[]> rows);
    Task<SavedDataSet?> LoadAsync(int id, string userId);
    Task<List<SavedDataSet>> ListByUserAsync(string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<string> ExportCsvAsync(int id, string userId);

    Task<SavedWorkspace> SaveWorkspaceAsync(string userId, string name, string workspaceType, string configurationJson);
    Task<SavedWorkspace?> LoadWorkspaceAsync(int id, string userId);
    Task<List<SavedWorkspace>> ListWorkspacesByUserAsync(string userId, string? workspaceType = null);
    Task<bool> DeleteWorkspaceAsync(int id, string userId);
}
