namespace TCalc.Web.Services;

public sealed class GeometryService : IGeometryService
{
    private readonly ILogger<GeometryService> _logger;

    public GeometryService(ILogger<GeometryService> logger) => _logger = logger;

    public GeometryResult Calculate(string shape, Dictionary<string, double> dims)
    {
        if (string.IsNullOrWhiteSpace(shape))
            return Fail("Shape is required.");

        foreach (var kv in dims)
        {
            if (kv.Value < 0)
                return Fail($"'{kv.Key}' must be non-negative.");
        }

        _logger.LogDebug("Calculating geometry for '{Shape}' with {ParamCount} dimensions", shape, dims.Count);

        return shape.ToLowerInvariant() switch
        {
            "circle"             => CalcCircle(dims),
            "triangle"           => CalcTriangle(dims),
            "rectangle"          => CalcRectangle(dims),
            "square"             => CalcSquare(dims),
            "trapezoid"          => CalcTrapezoid(dims),
            "parallelogram"      => CalcParallelogram(dims),
            "ellipse"            => CalcEllipse(dims),
            "sphere"             => CalcSphere(dims),
            "cylinder"           => CalcCylinder(dims),
            "cone"               => CalcCone(dims),
            "cube"               => CalcCube(dims),
            "rectangular prism"  => CalcRectPrism(dims),
            _ => Fail($"Unknown shape '{shape}'.")
        };
    }

    // ─── 2-D shapes ──────────────────────────────────────────

    private static GeometryResult CalcCircle(Dictionary<string, double> d)
    {
        if (!Get(d, "radius", out double r)) return Fail("Radius is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Circle",
            Area = Math.PI * r * r,
            Perimeter = 2 * Math.PI * r
        };
    }

    private static GeometryResult CalcTriangle(Dictionary<string, double> d)
    {
        if (!Get(d, "base", out double b)) return Fail("Base is required.");
        if (!Get(d, "height", out double h)) return Fail("Height is required.");

        double area = 0.5 * b * h;

        // Perimeter only if all three sides provided
        double? perimeter = null;
        if (Get(d, "sideA", out double a) && Get(d, "sideB", out double sb) && Get(d, "sideC", out double sc))
            perimeter = a + sb + sc;

        return new GeometryResult { Success = true, Shape = "Triangle", Area = area, Perimeter = perimeter };
    }

    private static GeometryResult CalcRectangle(Dictionary<string, double> d)
    {
        if (!Get(d, "length", out double l)) return Fail("Length is required.");
        if (!Get(d, "width", out double w)) return Fail("Width is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Rectangle",
            Area = l * w,
            Perimeter = 2 * (l + w)
        };
    }

    private static GeometryResult CalcSquare(Dictionary<string, double> d)
    {
        if (!Get(d, "side", out double s)) return Fail("Side is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Square",
            Area = s * s,
            Perimeter = 4 * s
        };
    }

    private static GeometryResult CalcTrapezoid(Dictionary<string, double> d)
    {
        if (!Get(d, "base1", out double b1)) return Fail("Base1 is required.");
        if (!Get(d, "base2", out double b2)) return Fail("Base2 is required.");
        if (!Get(d, "height", out double h)) return Fail("Height is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Trapezoid",
            Area = 0.5 * (b1 + b2) * h
        };
    }

    private static GeometryResult CalcParallelogram(Dictionary<string, double> d)
    {
        if (!Get(d, "base", out double b)) return Fail("Base is required.");
        if (!Get(d, "height", out double h)) return Fail("Height is required.");
        double? perimeter = null;
        if (Get(d, "side", out double s))
            perimeter = 2 * (b + s);
        return new GeometryResult
        {
            Success = true, Shape = "Parallelogram",
            Area = b * h,
            Perimeter = perimeter
        };
    }

    private static GeometryResult CalcEllipse(Dictionary<string, double> d)
    {
        if (!Get(d, "semiMajor", out double a)) return Fail("Semi-major axis (a) is required.");
        if (!Get(d, "semiMinor", out double b)) return Fail("Semi-minor axis (b) is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Ellipse",
            Area = Math.PI * a * b,
            Perimeter = Math.PI * (3 * (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b))) // Ramanujan approximation
        };
    }

    // ─── 3-D shapes ──────────────────────────────────────────

    private static GeometryResult CalcSphere(Dictionary<string, double> d)
    {
        if (!Get(d, "radius", out double r)) return Fail("Radius is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Sphere",
            Volume = (4.0 / 3.0) * Math.PI * r * r * r,
            SurfaceArea = 4 * Math.PI * r * r
        };
    }

    private static GeometryResult CalcCylinder(Dictionary<string, double> d)
    {
        if (!Get(d, "radius", out double r)) return Fail("Radius is required.");
        if (!Get(d, "height", out double h)) return Fail("Height is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Cylinder",
            Volume = Math.PI * r * r * h,
            SurfaceArea = 2 * Math.PI * r * (r + h)
        };
    }

    private static GeometryResult CalcCone(Dictionary<string, double> d)
    {
        if (!Get(d, "radius", out double r)) return Fail("Radius is required.");
        if (!Get(d, "height", out double h)) return Fail("Height is required.");
        double slant = Math.Sqrt(r * r + h * h);
        return new GeometryResult
        {
            Success = true, Shape = "Cone",
            Volume = (1.0 / 3.0) * Math.PI * r * r * h,
            SurfaceArea = Math.PI * r * (r + slant)
        };
    }

    private static GeometryResult CalcCube(Dictionary<string, double> d)
    {
        if (!Get(d, "side", out double s)) return Fail("Side is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Cube",
            Volume = s * s * s,
            SurfaceArea = 6 * s * s
        };
    }

    private static GeometryResult CalcRectPrism(Dictionary<string, double> d)
    {
        if (!Get(d, "length", out double l)) return Fail("Length is required.");
        if (!Get(d, "width", out double w)) return Fail("Width is required.");
        if (!Get(d, "height", out double h)) return Fail("Height is required.");
        return new GeometryResult
        {
            Success = true, Shape = "Rectangular Prism",
            Volume = l * w * h,
            SurfaceArea = 2 * (l * w + l * h + w * h)
        };
    }

    // ─── Helpers ──────────────────────────────────────────────

    private static bool Get(Dictionary<string, double> d, string key, out double value)
    {
        return d.TryGetValue(key, out value);
    }

    private static GeometryResult Fail(string error)
    {
        return new GeometryResult { Success = false, Error = error };
    }
}
