namespace TCalc.Web.Services;

/// <summary>
/// Calculates area, perimeter, volume, and surface area for geometric shapes.
/// </summary>
public interface IGeometryService
{
    GeometryResult Calculate(string shape, Dictionary<string, double> dimensions);
}

public sealed class GeometryResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string Shape { get; init; } = string.Empty;
    public double? Area { get; init; }
    public double? Perimeter { get; init; }
    public double? Volume { get; init; }
    public double? SurfaceArea { get; init; }
}
