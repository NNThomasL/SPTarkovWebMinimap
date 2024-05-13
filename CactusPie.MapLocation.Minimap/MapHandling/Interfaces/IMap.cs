using System.Collections.Generic;
using CactusPie.MapLocation.Minimap.MapHandling.Data;

namespace CactusPie.MapLocation.Minimap.MapHandling.Interfaces;

public interface IMap
{
    List<BoundData> CustomBounds { get; set; }

    string MapIdentifier { get; }

    string MapImageFile { get; }

    string MapName { get; }

    double MapRotation => 0.0f;

    IReadOnlyList<double> XCoefficients { get; set; }

    IReadOnlyList<double> ZCoefficients { get; set; }

    double MarkerScale { get; }

    double TransformXPosition(double mapXPosition);

    double TransformZPosition(double mapZPosition);

    void DeleteBoundData(string boundName);

    BoundData? GetBound(string boundName);

    TransformedPositionResult TransformPositions(double xPosition, double zPosition, double yPosition, bool useCustomBounds);
}