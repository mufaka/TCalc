using System.ComponentModel.DataAnnotations;

namespace TCalc.Web.Models;

/// <summary>
/// A single row in a <see cref="SavedDataSet"/>.
/// </summary>
public class DataRow
{
    public int Id { get; set; }

    public int DataSetId { get; set; }

    public int RowIndex { get; set; }

    /// <summary>JSON-serialised column values, e.g. ["25","88.5"].</summary>
    [Required]
    public string ValuesJson { get; set; } = "[]";

    public SavedDataSet DataSet { get; set; } = null!;
}
