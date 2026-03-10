using System.ComponentModel.DataAnnotations;

namespace TCalc.Web.Models;

/// <summary>
/// A user-owned data set persisted to the database.
/// </summary>
public class SavedDataSet
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>JSON-serialised column headers, e.g. ["Age","Score"].</summary>
    [MaxLength(4000)]
    public string? HeadersJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DataRow> Rows { get; set; } = [];
}
