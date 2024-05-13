using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Cors;
using EmbedIO.Files;
using TechHappy.MapLocation.Common.Requests;
using TechHappy.MapLocation.Common.Requests.Data;
using TechHappy.MapLocation.Services.Airdrop;
using TechHappy.MapLocation.Services.Bots;
using TechHappy.MapLocation.Services.Quests;
using UnityEngine;

namespace TechHappy.MapLocation.Services
{
    public sealed class MapDataServer : IMapDataServer
    {
        private readonly IBotDataService _botDataService;

        private readonly IAirdropService _airdropService;

        private readonly IQuestDataService _questDataService;

        private bool _isGameInProgress;

        private Player _player;

        private WebServer _server = null;

        public DateTime? LastQuestChangeTime { get; set; }

        public MapDataServer(IQuestDataService questDataService, IBotDataService botDataService, IAirdropService airdropService)
        {
            _questDataService = questDataService;
            _botDataService = botDataService;
            _airdropService = airdropService;
        }

        public void StartServer(string ipAddress, int port)
        {
            try
            {
                if (_server == null)
                {
                    var url = $"http://{ipAddress}:{port}/";
                    _server = CreateWebServer(url, port);
                }

                if (_server.State == WebServerState.Created)
                {
                    _server.Start();
                }
            }
            catch (Exception e)
            {
                MapLocationPlugin.MapLocationLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        public void OnGameStarted()
        {
            _airdropService.AirdropData = null;
            _botDataService.InitializeBotDataForCurrentGame();
            _questDataService.ReloadQuestData(ZoneDataHelper.GetAllTriggers());
            _isGameInProgress = true;
            _player = Singleton<GameWorld>.Instance.MainPlayer.GetComponentInChildren<GamePlayerOwner>().Player;
            LastQuestChangeTime = null;
        }

        public void OnGameEnded()
        {
            _player = null;
            _botDataService.UnloadBotDataForCurrentGame();
            _isGameInProgress = false;
            LastQuestChangeTime = null;
        }

        public void OnQuestsChanged(TriggerWithId[] allTriggers)
        {
            _questDataService?.ReloadQuestData(allTriggers);
        }

        public void Dispose()
        {
            _server?.Dispose();
            _botDataService?.Dispose();
        }

        private WebServer CreateWebServer(string url, int port)
        {
            string zipPath = @"webData\";
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string combinedPath = Path.Combine(assemblyPath, zipPath);
            
            MapLocationPlugin.MapLocationLogger.LogInfo($"Starting web server with zip at {combinedPath}");
            
            WebServer server = new WebServer(port)
                .WithLocalSessionManager()
                .WithModule(new CorsModule("/mapData", "*"))
                .WithModule(new CorsModule("/quests", "*"))
                .WithModule(new ActionModule("/mapData", HttpVerbs.Get, GetMapData))
                .WithModule(new ActionModule("/quests", HttpVerbs.Get, GetQuests))
                .WithModule(new FileModule("/",
                new FileSystemProvider(combinedPath, true)));

            return server;
        }

        private Task GetQuests(IHttpContext context)
        {
            if (!_isGameInProgress)
            {
                return context.SendDataAsync(
                    new QuestDataResponse
                    {
                        Quests = Array.Empty<QuestData>(),
                    });
            }

            IReadOnlyList<QuestData> quests = _questDataService.QuestMarkers;
            var response = new QuestDataResponse
            {
                Quests = quests,
            };

            return context.SendDataAsync(response);
        }

        private Task GetMapData(IHttpContext ctx)
        {
            if (!_isGameInProgress || _player == null)
            {
                return ctx.SendDataAsync(new MapLocationResponse());
            }

            string mapName = _player.Location;
            Vector3 playerPosition = _player.Position;
            Vector2 playerRotation = _player.Rotation;

            List<BotData> botLocations = _botDataService.SpawnedBots.Values.Select(
                    bot => new BotData
                    {
                        BotId = bot.Id,
                        BotType = _botDataService.GetBotType(bot),
                        XPosition = bot.Position.x,
                        YPosition = bot.Position.y,
                        ZPosition = bot.Position.z,
                    })
                .ToList();

            var response = new MapLocationResponse
            {
                MapName = mapName,
                XPosition = playerPosition.x,
                YPosition = playerPosition.y,
                ZPosition = playerPosition.z,
                XRotation = playerRotation.x,
                YRotation = playerRotation.y,
                IsGameInProgress = _isGameInProgress,
                Airdrop = _airdropService.AirdropData,
                BotLocations = botLocations,
                LastQuestChangeTime = LastQuestChangeTime,
            };

            return ctx.SendDataAsync(response);
        }
    }
}