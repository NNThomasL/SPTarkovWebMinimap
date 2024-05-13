using System.Collections.Generic;

namespace CactusPie.MapLocation.Minimap.MapHandling.Data;

public sealed class MapData
{
    public List<BoundData>? CustomBounds { get; set; }

    public string? MapIdentifier { get; init; }

    public string? MapImageFile { get; init; }

    public string? MapName { get; init; }

    public double MapRotation { get; init; }

    public double MarkerScale { get; init; }

    public IReadOnlyList<double>? XCoefficients { get; init; }

    public IReadOnlyList<double>? ZCoefficients { get; init; }
}