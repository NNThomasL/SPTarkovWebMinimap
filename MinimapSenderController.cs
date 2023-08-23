using System;
using System.Net;
using Comfort.Common;
using EFT;
using JetBrains.Annotations;
using UnityEngine;

namespace TechHappy.MinimapSender
{
    public class MinimapSenderController : MonoBehaviour
    {
        private MinimapSenderBroadcastService _minimapSenderService;

        [UsedImplicitly]
        public void Start()
        {
            var gamePlayerOwner = GetLocalPlayerFromWorld().GetComponentInChildren<GamePlayerOwner>();
            MinimapSenderPlugin.RefreshIntervalMillieconds.SettingChanged += RefreshIntervalSecondsOnSettingChanged;

            if (_minimapSenderService == null)
            {
                _minimapSenderService = new MinimapSenderBroadcastService(gamePlayerOwner);
            }

            _minimapSenderService.StartBroadcastingPosition(MinimapSenderPlugin.RefreshIntervalMillieconds.Value);
        }

        private void RefreshIntervalSecondsOnSettingChanged(object sender, EventArgs e)
        {
            _minimapSenderService.ChangeInterval(MinimapSenderPlugin.RefreshIntervalMillieconds.Value);
        }

        [UsedImplicitly]
        public void Stop()
        {
            _minimapSenderService?.StopBroadcastingPosition();
        }

        private Player GetLocalPlayerFromWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.MainPlayer == null)
            {
                return null;
            }

            return gameWorld.MainPlayer;
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            _minimapSenderService.Dispose();
            Destroy(this);
        }
    }
}