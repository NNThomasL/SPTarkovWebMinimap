using System;

namespace CactusPie.MapLocation.Minimap.Events;

public class MapAddedEventArgs : EventArgs
{
    public MapAddedEventArgs(
        string mapName,
        string mapIdentifier,
        string mapImagePath,
        double mapRotation,
        double markerScale)
    {
        MapName = mapName;
        MapIdentifier = mapIdentifier;
        MapImagePath = mapImagePath;
        MapRotation = mapRotation;
        MarkerScale = markerScale;
    }

    public string MapIdentifier { get; set; }

    public string MapImagePath { get; set; }

    public string MapName { get; set; }

    public double MapRotation { get; set; }

    public double MarkerScale { get; set; }
}