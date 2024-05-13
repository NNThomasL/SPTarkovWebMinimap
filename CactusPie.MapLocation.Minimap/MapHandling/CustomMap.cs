using System;
using System.Collections.Generic;
using System.Linq;
using CactusPie.MapLocation.Minimap.Helpers;
using CactusPie.MapLocation.Minimap.MapHandling.Data;
using CactusPie.MapLocation.Minimap.MapHandling.Interfaces;

namespace CactusPie.MapLocation.Minimap.MapHandling;

public sealed class CustomMap : IMap
{
    public CustomMap(MapData mapData)
    {
        ArgumentNullException.ThrowIfNull(mapData.MapName);
        ArgumentNullException.ThrowIfNull(mapData.MapIdentifier);
        ArgumentNullException.ThrowIfNull(mapData.MapImageFile);
        ArgumentNullException.ThrowIfNull(mapData.XCoefficients);
        ArgumentNullException.ThrowIfNull(mapData.ZCoefficients);

        MapName = mapData.MapName;
        MapIdentifier = mapData.MapIdentifier;
        MapImageFile = mapData.MapImageFile;
        XCoefficients = mapData.XCoefficients;
        ZCoefficients = mapData.ZCoefficients;
        MapRotation = mapData.MapRotation;
        MarkerScale = mapData.MarkerScale <= 0 ? 1 : mapData.MarkerScale;
        CustomBounds = mapData.CustomBounds ?? new List<BoundData>();
    }

    public string MapImageFile { get; }

    public string MapName { get; }

    public string MapIdentifier { get; }

    public IReadOnlyList<double> XCoefficients { get; set; }

    public IReadOnlyList<double> ZCoefficients { get; set; }

    public double MapRotation { get; }

    public double MarkerScale { get; }

    public List<BoundData> CustomBounds { get; set; }

    public double TransformXPosition(double mapXPosition)
    {
        return PolynomialHelper.CalculatePolynomialValue(mapXPosition, XCoefficients);
    }

    public double TransformZPosition(double mapZPosition)
    {
        return PolynomialHelper.CalculatePolynomialValue(mapZPosition, ZCoefficients);
    }

    public void DeleteBoundData(string boundName)
    {
        int boundIndex = CustomBounds.FindIndex(x => x.BoundName == boundName);
        CustomBounds.RemoveAt(boundIndex);
    }

    public BoundData? GetBound(string boundName)
    {
        return CustomBounds.FirstOrDefault(x => x.BoundName == boundName);
    }

    public TransformedPositionResult TransformPositions(
        double xPosition,
        double zPosition,
        double yPosition,
        bool useCustomBounds)
    {
        double transformedXPosition;
        double transformedZPosition;

        if (useCustomBounds)
        {
            foreach (BoundData customBound in CustomBounds)
            {
                if (customBound.IsValidForGamePosition(
                        xPosition,
                        zPosition,
                        yPosition))
                {
                    transformedXPosition = customBound.TransformXPosition(xPosition);
                    transformedZPosition = customBound.TransformZPosition(zPosition);
                    return new TransformedPositionResult(transformedXPosition, transformedZPosition);
                }
            }
        }

        transformedXPosition = TransformXPosition(xPosition);
        transformedZPosition = TransformZPosition(zPosition);
        return new TransformedPositionResult(transformedXPosition, transformedZPosition);
    }
}