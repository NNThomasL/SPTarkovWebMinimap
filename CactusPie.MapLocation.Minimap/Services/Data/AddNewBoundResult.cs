using CactusPie.MapLocation.Minimap.MapHandling.Data;

namespace CactusPie.MapLocation.Minimap.Services.Data;

public sealed class AddNewBoundResult
{
    public string? ErrorMessage { get; init; }

    public MapData? NewMapData { get; init; }

    public bool Success { get; init; }
}