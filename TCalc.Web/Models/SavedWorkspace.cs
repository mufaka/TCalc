using System.ComponentModel.DataAnnotations;

namespace TCalc.Web.Models;

/// <summary>
/// A saved graphing or statistics workspace for an authenticated user.
/// </summary>
public class SavedWorkspace
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string WorkspaceType { get; set; } = "Statistics";

    /// <summary>JSON blob storing workspace-specific configuration (equations, settings, etc.).</summary>
    public string ConfigurationJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
