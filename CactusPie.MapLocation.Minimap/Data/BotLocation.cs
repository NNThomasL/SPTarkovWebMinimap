using TechHappy.MapLocation.Common.Requests.Data;

namespace CactusPie.MapLocation.Minimap.Data;

public class BotLocation
{
    public BotLocation(int botId, BotType botType, float xPosition, float yPosition, float zPosition)
    {
        BotId = botId;
        BotType = botType;
        XPosition = xPosition;
        YPosition = yPosition;
        ZPosition = zPosition;
    }

    public int BotId { get; }

    public BotType BotType { get; }

    public float XPosition { get; }

    public float YPosition { get; }

    public float ZPosition { get; }
}