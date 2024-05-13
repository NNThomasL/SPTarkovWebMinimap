using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using TechHappy.MapLocation.Common.Requests.Data;

namespace TechHappy.MapLocation.Services.Bots
{
    public sealed class BotDataService : IBotDataService
    {
        private readonly Dictionary<int, BotOwner> _spawnedBots = new Dictionary<int, BotOwner>(35);

        private BotSpawner _botSpawnerClass;

        private bool _isInitialized = false;

        public IReadOnlyDictionary<int, BotOwner> SpawnedBots => _spawnedBots;

        public BotType GetBotType(BotOwner bot)
        {
            InfoClass info = bot.Profile.Info;

            if (info.Side == EPlayerSide.Usec)
            {
                return BotType.Usec;
            }

            if (info.Side == EPlayerSide.Bear)
            {
                return BotType.Bear;
            }

            if (info.Settings.IsBossOrFollower())
            {
                return BotType.Boss;
            }

            if (info.Side == EPlayerSide.Savage)
            {
                return BotType.Scav;
            }

            return BotType.Other;
        }

        public void InitializeBotDataForCurrentGame()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException(
                    $"The service cannot be initialized twice. Run {nameof(UnloadBotDataForCurrentGame)} first.");
            }

            if (Singleton<IBotGame>.Instantiated)
            {
                _botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

                _botSpawnerClass.OnBotCreated += OnBotCreated;
                _botSpawnerClass.OnBotRemoved += OnBotRemoved;
                _isInitialized = true;
            }
        }

        public void UnloadBotDataForCurrentGame()
        {
            _botSpawnerClass.OnBotCreated -= OnBotCreated;
            _botSpawnerClass.OnBotRemoved -= OnBotRemoved;
            _botSpawnerClass = null;
            _spawnedBots.Clear();
            _isInitialized = false;
        }

        public void Dispose()
        {
            if (_botSpawnerClass != null)
            {
                _botSpawnerClass.OnBotCreated -= OnBotCreated;
                _botSpawnerClass.OnBotRemoved -= OnBotRemoved;
                _botSpawnerClass = null;
            }
        }

        private void OnBotRemoved(BotOwner botOwner)
        {
            _spawnedBots.Remove(botOwner.Id);
        }

        private void OnBotCreated(BotOwner botOwner)
        {
            _spawnedBots.Add(botOwner.Id, botOwner);
        }

        ~BotDataService()
        {
            Dispose();
        }
    }
}