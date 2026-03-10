using Microsoft.Extensions.Logging.Abstractions;
using TCalc.Web.Services;

namespace TCalc.Tests;

public class GeometryServiceTests
{
    private readonly GeometryService _service = new(NullLogger<GeometryService>.Instance);

    // ─── Circle ──────────────────────────────────────────────

    [Fact]
    public void Circle_CalculatesAreaAndPerimeter()
    {
        var result = _service.Calculate("circle", new() { ["radius"] = 5 });
        Assert.True(result.Success);
        Assert.Equal(Math.PI * 25, result.Area!.Value, 10);
        Assert.Equal(2 * Math.PI * 5, result.Perimeter!.Value, 10);
    }

    [Fact]
    public void Circle_ZeroRadius()
    {
        var result = _service.Calculate("circle", new() { ["radius"] = 0 });
        Assert.True(result.Success);
        Assert.Equal(0, result.Area);
    }

    // ─── Rectangle ───────────────────────────────────────────

    [Fact]
    public void Rectangle_CalculatesAreaAndPerimeter()
    {
        var result = _service.Calculate("rectangle", new() { ["length"] = 4, ["width"] = 3 });
        Assert.True(result.Success);
        Assert.Equal(12, result.Area);
        Assert.Equal(14, result.Perimeter);
    }

    // ─── Square ──────────────────────────────────────────────

    [Fact]
    public void Square_CalculatesAreaAndPerimeter()
    {
        var result = _service.Calculate("square", new() { ["side"] = 5 });
        Assert.True(result.Success);
        Assert.Equal(25, result.Area);
        Assert.Equal(20, result.Perimeter);
    }

    // ─── Triangle ────────────────────────────────────────────

    [Fact]
    public void Triangle_CalculatesArea()
    {
        var result = _service.Calculate("triangle", new() { ["base"] = 10, ["height"] = 6 });
        Assert.True(result.Success);
        Assert.Equal(30, result.Area);
        Assert.Null(result.Perimeter); // no sides provided
    }

    // ─── Trapezoid ───────────────────────────────────────────

    [Fact]
    public void Trapezoid_CalculatesArea()
    {
        var result = _service.Calculate("trapezoid", new() { ["base1"] = 6, ["base2"] = 4, ["height"] = 5 });
        Assert.True(result.Success);
        Assert.Equal(25, result.Area);
    }

    // ─── Parallelogram ───────────────────────────────────────

    [Fact]
    public void Parallelogram_CalculatesArea()
    {
        var result = _service.Calculate("parallelogram", new() { ["base"] = 8, ["height"] = 5 });
        Assert.True(result.Success);
        Assert.Equal(40, result.Area);
    }

    // ─── Ellipse ─────────────────────────────────────────────

    [Fact]
    public void Ellipse_CalculatesArea()
    {
        var result = _service.Calculate("ellipse", new() { ["semiMajor"] = 5, ["semiMinor"] = 3 });
        Assert.True(result.Success);
        Assert.Equal(Math.PI * 15, result.Area!.Value, 10);
        Assert.NotNull(result.Perimeter);
    }

    // ─── Sphere ──────────────────────────────────────────────

    [Fact]
    public void Sphere_CalculatesVolumeAndSurfaceArea()
    {
        var result = _service.Calculate("sphere", new() { ["radius"] = 3 });
        Assert.True(result.Success);
        Assert.Equal((4.0 / 3.0) * Math.PI * 27, result.Volume!.Value, 10);
        Assert.Equal(4 * Math.PI * 9, result.SurfaceArea!.Value, 10);
    }

    // ─── Cylinder ────────────────────────────────────────────

    [Fact]
    public void Cylinder_CalculatesVolumeAndSurfaceArea()
    {
        var result = _service.Calculate("cylinder", new() { ["radius"] = 2, ["height"] = 10 });
        Assert.True(result.Success);
        Assert.Equal(Math.PI * 4 * 10, result.Volume!.Value, 10);
        Assert.Equal(2 * Math.PI * 2 * 12, result.SurfaceArea!.Value, 10);
    }

    // ─── Cone ────────────────────────────────────────────────

    [Fact]
    public void Cone_CalculatesVolumeAndSurfaceArea()
    {
        var result = _service.Calculate("cone", new() { ["radius"] = 3, ["height"] = 4 });
        Assert.True(result.Success);
        Assert.Equal((1.0 / 3.0) * Math.PI * 9 * 4, result.Volume!.Value, 10);
        double slant = Math.Sqrt(9 + 16);
        Assert.Equal(Math.PI * 3 * (3 + slant), result.SurfaceArea!.Value, 10);
    }

    // ─── Cube ────────────────────────────────────────────────

    [Fact]
    public void Cube_CalculatesVolumeAndSurfaceArea()
    {
        var result = _service.Calculate("cube", new() { ["side"] = 4 });
        Assert.True(result.Success);
        Assert.Equal(64, result.Volume);
        Assert.Equal(96, result.SurfaceArea);
    }

    // ─── Rectangular Prism ───────────────────────────────────

    [Fact]
    public void RectangularPrism_CalculatesVolumeAndSurfaceArea()
    {
        var result = _service.Calculate("rectangular prism", new() { ["length"] = 3, ["width"] = 4, ["height"] = 5 });
        Assert.True(result.Success);
        Assert.Equal(60, result.Volume);
        Assert.Equal(94, result.SurfaceArea);
    }

    // ─── Error cases ─────────────────────────────────────────

    [Fact]
    public void UnknownShape_ReturnsFail()
    {
        var result = _service.Calculate("hexagon", new());
        Assert.False(result.Success);
        Assert.Contains("Unknown", result.Error!);
    }

    [Fact]
    public void EmptyShape_ReturnsFail()
    {
        var result = _service.Calculate("", new());
        Assert.False(result.Success);
    }

    [Fact]
    public void NegativeDimension_ReturnsFail()
    {
        var result = _service.Calculate("circle", new() { ["radius"] = -1 });
        Assert.False(result.Success);
        Assert.Contains("non-negative", result.Error!);
    }

    [Fact]
    public void MissingRequiredDimension_ReturnsFail()
    {
        var result = _service.Calculate("circle", new());
        Assert.False(result.Success);
        Assert.Contains("Radius", result.Error!);
    }
}
