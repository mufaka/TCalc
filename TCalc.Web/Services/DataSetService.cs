using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TCalc.Web.Data;
using TCalc.Web.Models;

namespace TCalc.Web.Services;

/// <summary>
/// CRUD and CSV-export operations for user-owned data sets and workspaces.
/// </summary>
public sealed class DataSetService : IDataSetService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DataSetService> _logger;

    public DataSetService(ApplicationDbContext db, ILogger<DataSetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SavedDataSet> SaveAsync(
        string userId, string name, string? description,
        string[] headers, List<string[]> rows)
    {
        _logger.LogDebug("Saving data set '{Name}' for user {UserId} with {RowCount} rows", name, userId, rows.Count);
        var dataSet = new SavedDataSet
        {
            UserId = userId,
            Name = name,
            Description = description,
            HeadersJson = JsonSerializer.Serialize(headers),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        for (int i = 0; i < rows.Count; i++)
        {
            dataSet.Rows.Add(new DataRow
            {
                RowIndex = i,
                ValuesJson = JsonSerializer.Serialize(rows[i]),
            });
        }

        _db.SavedDataSets.Add(dataSet);
        await _db.SaveChangesAsync();
        return dataSet;
    }

    public async Task<SavedDataSet?> LoadAsync(int id, string userId)
    {
        return await _db.SavedDataSets
            .Include(d => d.Rows.OrderBy(r => r.RowIndex))
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
    }

    public async Task<List<SavedDataSet>> ListByUserAsync(string userId)
    {
        return await _db.SavedDataSets
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        _logger.LogDebug("Deleting data set {Id} for user {UserId}", id, userId);
        var dataSet = await _db.SavedDataSets
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (dataSet is null) return false;

        _db.SavedDataSets.Remove(dataSet);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string> ExportCsvAsync(int id, string userId)
    {
        var dataSet = await LoadAsync(id, userId);
        if (dataSet is null) return string.Empty;

        string[]? headers = dataSet.HeadersJson is not null
            ? JsonSerializer.Deserialize<string[]>(dataSet.HeadersJson)
            : null;

        using var writer = new StringWriter();

        if (headers is { Length: > 0 })
        {
            writer.WriteLine(string.Join(",", headers.Select(EscapeCsvField)));
        }

        foreach (var row in dataSet.Rows)
        {
            string[]? values = JsonSerializer.Deserialize<string[]>(row.ValuesJson);
            if (values is not null)
                writer.WriteLine(string.Join(",", values.Select(EscapeCsvField)));
        }

        return writer.ToString();
    }

    // ─── Workspace operations ──────────────────────────────────

    public async Task<SavedWorkspace> SaveWorkspaceAsync(
        string userId, string name, string workspaceType, string configurationJson)
    {
        _logger.LogDebug("Saving {WorkspaceType} workspace '{Name}' for user {UserId}", workspaceType, name, userId);
        var workspace = new SavedWorkspace
        {
            UserId = userId,
            Name = name,
            WorkspaceType = workspaceType,
            ConfigurationJson = configurationJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.SavedWorkspaces.Add(workspace);
        await _db.SaveChangesAsync();
        return workspace;
    }

    public async Task<SavedWorkspace?> LoadWorkspaceAsync(int id, string userId)
    {
        return await _db.SavedWorkspaces
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
    }

    public async Task<List<SavedWorkspace>> ListWorkspacesByUserAsync(string userId, string? workspaceType = null)
    {
        var query = _db.SavedWorkspaces.Where(w => w.UserId == userId);
        if (workspaceType is not null)
            query = query.Where(w => w.WorkspaceType == workspaceType);

        return await query.OrderByDescending(w => w.UpdatedAt).ToListAsync();
    }

    public async Task<bool> DeleteWorkspaceAsync(int id, string userId)
    {
        _logger.LogDebug("Deleting workspace {Id} for user {UserId}", id, userId);
        var workspace = await _db.SavedWorkspaces
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (workspace is null) return false;

        _db.SavedWorkspaces.Remove(workspace);
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── helpers ────────────────────────────────────────────────

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }
}
