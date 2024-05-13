using System;
using TechHappy.MapLocation.DI;
using JetBrains.Annotations;
using TechHappy.MapLocation.Services;
using UnityEngine;

namespace TechHappy.MapLocation
{
    public sealed class MapLocationController : MonoBehaviour
    {
        private IMapDataServer _mapDataServer;

        [UsedImplicitly]
        public void Start()
        {
            try
            {
                MapLocationPlugin.MapLocationLogger.LogInfo("Starting map server");
                _mapDataServer = MapLocationPlugin.ServiceContainer.GetInstance<IMapDataServer>();
                _mapDataServer.StartServer(MapLocationPlugin.ListenIpAddress.Value, MapLocationPlugin.ListenPort.Value);
                _mapDataServer.OnGameStarted();
                MapLocationPlugin.MapLocationLogger.LogInfo("Map server started");
            }
            catch (Exception e)
            {
                MapLocationPlugin.MapLocationLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            try
            {
                _mapDataServer.OnGameEnded();
                Destroy(this);
            }
            catch (Exception e)
            {
                MapLocationPlugin.MapLocationLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}