using System.Net;
using System.Net.Sockets;
using TechHappy.MapLocation.Common.Requests.Data;
using Comfort.Common;
using EFT;
using TechHappy.MapLocation.Services;
using TechHappy.MapLocation.Services.Airdrop;
using TechHappy.MapLocation.Services.Bots;
using TechHappy.MapLocation.Services.Quests;

namespace TechHappy.MapLocation.DI
{
    public static class ContainerBuilder
    {
        public static ServiceContainer BuildContainer()
        {
            var container = new ServiceContainer();

            container.Register<ILocalizationHelper, LocalizationHelper>();
            container.Register<IQuestDataService, QuestDataService>();
            container.Register<IBotDataService, BotDataService>();
            container.Register<IMapDataServer, MapDataServer>(new PerContainerLifetime());
            container.Register<IAirdropService, AirdropService>(new PerContainerLifetime());
            container.Register(_ => Singleton<GameWorld>.Instance);

            container.Compile();

            return container;
        }
    }
}