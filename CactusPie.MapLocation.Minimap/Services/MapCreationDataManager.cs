using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Helpers;
using CactusPie.MapLocation.Minimap.MapHandling.Data;
using CactusPie.MapLocation.Minimap.MapHandling.Interfaces;
using CactusPie.MapLocation.Minimap.Services.Data;
using CactusPie.MapLocation.Minimap.Services.Interfaces;

namespace CactusPie.MapLocation.Minimap.Services;

public class MapCreationDataManager : IMapCreationDataManager
{
    public MapCoefficientsGenerationResult GenerateMapCoefficients(string mapPositionMappings, int polynomialDegree)
    {
        if (string.IsNullOrEmpty(mapPositionMappings))
        {
            return new MapCoefficientsGenerationResult(null, false, "Map coefficients cannot be empty");
        }

        string[] lines = mapPositionMappings.Split("\n");

        var gameXPositions = new List<double>(lines.Length);
        var mapXPositions = new List<double>(lines.Length);
        var gameZPositions = new List<double>(lines.Length);
        var mapZPositions = new List<double>(lines.Length);

        for (var lineNumber = 0; lineNumber < lines.Length; lineNumber++)
        {
            string line = lines[lineNumber];

            string[] split = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 4)
            {
                continue;
            }

            if (!double.TryParse(split[0], out double gameXPosition))
            {
                return new MapCoefficientsGenerationResult(
                    null,
                    false,
                    $"Failed to parse first coordinate at line {lineNumber + 1}");
            }

            if (!double.TryParse(split[1], out double mapXPosition))
            {
                return new MapCoefficientsGenerationResult(
                    null,
                    false,
                    $"Failed to parse second coordinate at line {lineNumber + 1}");
            }

            if (!double.TryParse(split[2], out double gameZPosition))
            {
                return new MapCoefficientsGenerationResult(
                    null,
                    false,
                    $"Failed to parse third coordinate at line {lineNumber + 1}");
            }

            if (!double.TryParse(split[3], out double mapZPosition))
            {
                return new MapCoefficientsGenerationResult(
                    null,
                    false,
                    $"Failed to parse fourth coordinate at line {lineNumber + 1}");
            }

            gameXPositions.Add(gameXPosition);
            mapXPositions.Add(mapXPosition);
            gameZPositions.Add(gameZPosition);
            mapZPositions.Add(mapZPosition);
        }

        if (gameXPositions.Count <= 1)
        {
            return new MapCoefficientsGenerationResult(null, false, "You need to add at least 2 sets of map coordinates first");
        }

        if (polynomialDegree > 1)
        {
            ExtrapolateExtraMapPoints(mapXPositions, gameXPositions);
            ExtrapolateExtraMapPoints(mapZPositions, gameZPositions);
        }

        double[] gameXPositionsArray = gameXPositions.ToArray();
        double[] mapXPositionsArray = mapXPositions.ToArray();
        double[] gameZPositionsArray = gameZPositions.ToArray();
        double[] mapZPositionsArray = mapZPositions.ToArray();

        double[] xCoefficientsResult = PolynomialHelper.FitPolynomial(gameXPositionsArray, mapXPositionsArray, polynomialDegree);
        double[] zCoefficientsResult = PolynomialHelper.FitPolynomial(gameZPositionsArray, mapZPositionsArray, polynomialDegree);

        var coefficients = new MapCoefficients
        {
            XCoefficients = xCoefficientsResult,
            ZCoefficients = zCoefficientsResult,
            GameXPositionsArray = gameXPositionsArray,
            MapXPositionsArray = mapXPositionsArray,
            GameZPositionsArray = gameZPositionsArray,
            MapZPositionsArray = mapZPositionsArray,
        };

        return new MapCoefficientsGenerationResult(coefficients, true);
    }

    public (bool Success, string? ErrorMessage) AddNewMap(
        string mapName,
        string mapIdentifier,
        double mapRotation,
        double markerScale,
        string mapImagePath)
    {
        string mapImageFileName = Path.GetFileName(mapImagePath);

        string newMapConfigFilePath = GetMapConfigFilePath(mapName);
        string newMapImageDestinationFilePath = PathHelper.GetAbsolutePath($@"Maps\Images\{mapImageFileName}");

        if (File.Exists(newMapConfigFilePath))
        {
            return (false, "A map with that name already exists");
        }

        var mapData = new MapData
        {
            MapName = mapName,
            MapIdentifier = mapIdentifier,
            MapRotation = mapRotation,
            MapImageFile = mapImageFileName,
            MarkerScale = markerScale,
            XCoefficients = Array.Empty<double>(),
            ZCoefficients = Array.Empty<double>(),
            CustomBounds = new List<BoundData>(),
        };

        string serializedMapData = JsonSerializer.Serialize(
            mapData,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(newMapConfigFilePath, serializedMapData);
        File.Copy(mapImagePath, newMapImageDestinationFilePath, true);

        return (true, null);
    }

    public AddNewBoundResult AddNewBound(string mapName, BoundData bound)
    {
        string mapConfigFilePath = GetMapConfigFilePath(mapName);

        if (!File.Exists(mapConfigFilePath))
        {
            return new AddNewBoundResult
            {
                Success = false,
                ErrorMessage = $"Map file {mapConfigFilePath} doesn't exist",
            };
        }

        string boundFileName = @$"Maps\MapCreationData\Bounds\{mapName}\{bound.BoundName}.txt";

        if (File.Exists(boundFileName))
        {
            return new AddNewBoundResult
            {
                Success = false,
                ErrorMessage = $"File {boundFileName} already exists",
            };
        }

        string boundDirectory = Path.GetDirectoryName(boundFileName)!;
        Directory.CreateDirectory(boundDirectory);
        File.Create(boundFileName).Dispose();

        string fileContent = File.ReadAllText(mapConfigFilePath);
        MapData? mapData = JsonSerializer.Deserialize<MapData>(fileContent);

        mapData!.CustomBounds ??= new List<BoundData>();
        mapData.CustomBounds.Add(bound);

        fileContent = JsonSerializer.Serialize(
            mapData,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(mapConfigFilePath, fileContent);

        return new AddNewBoundResult
        {
            Success = true,
            NewMapData = mapData,
        };
    }

    public void SaveMapPositionData(IMap selectedMap, string positionMappings, string? selectedBound)
    {
        string positionsPath;

        if (selectedBound != null)
        {
            positionsPath = PathHelper.GetAbsolutePath($@"Maps\MapCreationData\Bounds\{selectedMap.MapName}\{selectedBound}.txt");
        }
        else
        {
            positionsPath = PathHelper.GetAbsolutePath($@"Maps\MapCreationData\{selectedMap.MapName}.txt");
        }

        if (!File.Exists(positionsPath))
        {
            string directoryName = Path.GetDirectoryName(positionsPath) ??
                                   throw new InvalidOperationException($"Invalid path: {positionsPath}");

            Directory.CreateDirectory(directoryName);
            File.Create(positionsPath).Dispose();
        }

        File.WriteAllText(positionsPath, positionMappings);
    }

    public string? GetMapPositionData(IMap map, string? selectedBound)
    {
        string positionsPath;

        if (selectedBound != null)
        {
            positionsPath = PathHelper.GetAbsolutePath($@"Maps\MapCreationData\Bounds\{map.MapName}\{selectedBound}.txt");
        }
        else
        {
            positionsPath = PathHelper.GetAbsolutePath($@"Maps\MapCreationData\{map.MapName}.txt");
        }


        if (!File.Exists(positionsPath))
        {
            return null;
        }

        string positionData = File.ReadAllText(positionsPath);
        return positionData;
    }

    public void UpdateMap(MapData mapData)
    {
        string newMapFileNamePath = PathHelper.GetAbsolutePath($@"Maps\{mapData.MapName}.json");

        string serializedMapData = JsonSerializer.Serialize(
            mapData,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(newMapFileNamePath, serializedMapData);
    }

    public void RemoveBoundFile(IMap map, string bound)
    {
        string mapCreationDataFile = PathHelper.GetAbsolutePath($@"Maps\MapCreationData\Bounds\{map.MapName}\{bound}.txt");
        if (File.Exists(mapCreationDataFile))
        {
            File.Delete(mapCreationDataFile);
        }
    }

    public void SaveMapData(IMap map)
    {
        string mapConfigFilePath = PathHelper.GetAbsolutePath($@"Maps\{map.MapName}.json");

        string fileContent = JsonSerializer.Serialize(
            map,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(mapConfigFilePath, fileContent);
    }

    private static string GetMapConfigFilePath(string mapName)
    {
        string newMapFileNamePath = PathHelper.GetAbsolutePath($@"Maps\{mapName}.json");
        return newMapFileNamePath;
    }

    // Fits a linear function and based on the provided values and adds some extra points beyond the maximum and below the minimum value.
    // Will avoid the function shooting up at the edges of the map
    // Makes no sense to use it if we're fitting a linear function (1st degree polynomial) in the first place
    private static void ExtrapolateExtraMapPoints(List<double> gamePositions, List<double> mapPositions)
    {
        double[] gamePositionsArray = gamePositions.ToArray();
        double[] mapPositionsArray = mapPositions.ToArray();

        double[] coefficients = PolynomialHelper.FitPolynomial(gamePositionsArray, mapPositionsArray, 1);

        double maxGamePosition = gamePositionsArray.Max();

        for (int i = 0; i < 10; i++)
        {
            maxGamePosition += 10;
            double newMapXPosition = PolynomialHelper.CalculatePolynomialValue(maxGamePosition, coefficients);
            gamePositions.Add(maxGamePosition);
            mapPositions.Add(newMapXPosition);
        }

        double minGamePosition = gamePositions.Min();

        for (int i = 0; i < 10; i++)
        {
            minGamePosition -= 10;
            double newMapPosition = PolynomialHelper.CalculatePolynomialValue(minGamePosition, coefficients);
            gamePositions.Add(minGamePosition);
            mapPositions.Add(newMapPosition);
        }
    }
}