using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.MapHandling.Data;
using CactusPie.MapLocation.Minimap.MapHandling.Interfaces;
using CactusPie.MapLocation.Minimap.Services.Data;

namespace CactusPie.MapLocation.Minimap.Services.Interfaces;

public interface IMapCreationDataManager
{
    MapCoefficientsGenerationResult GenerateMapCoefficients(string mapPositionMappings, int polynomialDegree);

    (bool Success, string? ErrorMessage) AddNewMap(
        string mapName,
        string mapIdentifier,
        double mapRotation,
        double markerScale,
        string mapImagePath);

    AddNewBoundResult AddNewBound(string mapName, BoundData bound);

    void SaveMapPositionData(IMap selectedMap, string positionMappings, string? selectedBound);

    string? GetMapPositionData(IMap map, string? selectedBound);

    void UpdateMap(MapData mapData);

    void RemoveBoundFile(IMap map, string bound);

    void SaveMapData(IMap map);
}