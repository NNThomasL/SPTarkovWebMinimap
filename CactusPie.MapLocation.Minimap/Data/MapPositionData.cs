using System;
using System.Collections.Generic;
using TechHappy.MapLocation.Common.Requests.Data;

namespace CactusPie.MapLocation.Minimap.Data;

public class MapPositionData
{
    public MapPositionData(
        string? mapIdentifier,
        float xPosition,
        float yPosition,
        float zPosition,
        float xRotation,
        float yRotation,
        AirdropData airdropData,
        DateTime? lastQuestChangeTime,
        List<BotLocation> botLocations)
    {
        MapIdentifier = mapIdentifier;
        XPosition = xPosition;
        YPosition = yPosition;
        ZPosition = zPosition;
        XRotation = xRotation;
        YRotation = yRotation;
        AirdropData = airdropData;
        LastQuestChangeTime = lastQuestChangeTime;
        BotLocations = botLocations;
    }

    public List<BotLocation>? BotLocations { get; }

    public string? MapIdentifier { get; }

    public float XPosition { get; }

    public float XRotation { get; }

    public float YPosition { get; }

    public float YRotation { get; }

    public AirdropData AirdropData { get; }

    public DateTime? LastQuestChangeTime { get; }

    public float ZPosition { get; }
}