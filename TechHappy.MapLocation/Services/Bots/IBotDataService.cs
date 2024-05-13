using System;
using System.Collections.Generic;
using EFT;
using TechHappy.MapLocation.Common.Requests.Data;

namespace TechHappy.MapLocation.Services.Bots
{
    public interface IBotDataService : IDisposable
    {
        IReadOnlyDictionary<int, BotOwner> SpawnedBots { get; }

        BotType GetBotType(BotOwner bot);

        void InitializeBotDataForCurrentGame();

        void UnloadBotDataForCurrentGame();
    }
}